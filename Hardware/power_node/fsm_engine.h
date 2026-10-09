#ifndef FSM_ENGINE_H
#define FSM_ENGINE_H

#include "config.h"

// Pure C++ (no Arduino calls) so it can be unit-tested on a PC and ported to ESP-IDF unchanged.

inline SystemInputs make_system_inputs(const PotInputs& pot, const WebControls& web) {
    SystemInputs s;
    s.solar_kw        = pot.solar_kw;
    s.wind_kw         = pot.wind_kw;
    s.battery_soc_pct = pot.battery_soc_pct;
    s.demand_kw       = web.demand_kw;
    s.hour            = web.hour;
    s.is_peak_tariff  = (web.hour >= PEAK_START_HOUR && web.hour <= PEAK_END_HOUR);
    return s;
}

// Finite state machine. State = which supply mode is currently selected:
// solar, wind, solar+wind together, battery, or grid.
//
//  Layer 1 (every tick): which sources can cover demand + margin, and how suitable is each.
//  Layer 2 (state logic): hysteresis, score margin, dwell timer, safety override.
class FsmEngine {
public:
    Decision step(const SystemInputs& in, uint32_t now_ms) {
        Decision d = Decision();

        const float demand      = in.demand_kw < DEMAND_FLOOR_KW ? DEMAND_FLOOR_KW : in.demand_kw;
        const float need_engage = demand * (1.0f + ENGAGE_MARGIN);
        const float need_hold   = demand * (1.0f + HOLD_MARGIN);
        d.required_kw = need_engage;

        // ---- 1. Eligibility (hysteresis: lower bar for the source already selected) ----
        for (int i = 0; i < SRC_COUNT; i++) {
            const bool  is_current = started_ && ((int)current_ == i);
            const float need       = is_current ? need_hold : need_engage;
            bool ok = false;
            switch (i) {
                case SRC_SOLAR: ok = in.solar_kw >= need; break;
                case SRC_WIND:  ok = in.wind_kw  >= need; break;
                case SRC_BATTERY: {
                    const float min_soc = is_current ? SOC_FLOOR_PCT : SOC_ENGAGE_PCT;
                    ok = (in.battery_soc_pct >= min_soc) && (BATT_MAX_DISCHARGE_KW >= need);
                    break;
                }
                case SRC_SOLAR_WIND: ok = (in.solar_kw + in.wind_kw) >= need; break;
                default: ok = true; break;   // grid can always supply
            }
            d.eligible[i] = ok;
            d.score[i]    = ok ? suitability((Source)i, in, demand) : 0.0f;
        }

        // ---- 1b. Solar+Wind is the fallback tier: while a single renewable already covers demand,
        //          show the combo just below the best single one so it is never preferred over it. ----
        if (d.eligible[SRC_SOLAR] || d.eligible[SRC_WIND]) {
            float best_solo = d.eligible[SRC_SOLAR] ? d.score[SRC_SOLAR] : 0.0f;
            if (d.eligible[SRC_WIND] && d.score[SRC_WIND] > best_solo) best_solo = d.score[SRC_WIND];
            if (d.score[SRC_SOLAR_WIND] > best_solo - HYBRID_BELOW_SOLO_GAP) {
                d.score[SRC_SOLAR_WIND] = best_solo - HYBRID_BELOW_SOLO_GAP;
            }
        }

        // ---- 2. Best eligible source (ties -> lower index) ----
        Source best = SRC_GRID;
        float  best_score = -1.0f;
        for (int i = 0; i < SRC_COUNT; i++) {
            if (d.eligible[i] && d.score[i] > best_score) {
                best_score = d.score[i];
                best = (Source)i;
            }
        }
        d.best = best;

        // ---- 3. State transition ----
        if (!started_) {
            current_ = best; started_ = true; last_switch_ms_ = now_ms;
            reason_ = REASON_INIT;
        } else if (!d.eligible[current_]) {
            // Safety: current source can no longer supply demand (or battery hit SOC floor).
            // Bypasses the dwell timer.
            current_ = best; last_switch_ms_ = now_ms;
            reason_ = REASON_SAFETY;
        } else if (best != current_ &&
                   (steps_down_from_combo(current_, best) ||
                    d.score[best] >= d.score[current_] + SCORE_SWITCH_MARGIN)) {
            const uint32_t elapsed = now_ms - last_switch_ms_;
            if (elapsed >= DWELL_MS) {
                current_ = best; last_switch_ms_ = now_ms;
                reason_ = REASON_SCORE;
            } else {
                d.holding = true;
                d.dwell_remaining_ms = DWELL_MS - elapsed;
            }
        }
        d.selected = current_;
        d.reason   = reason_;

        // ---- 4. Surplus handling: charge battery if it needs it, else export to grid ----
        if (in.battery_soc_pct >= SOC_FULL_PCT)            batt_full_ = true;
        else if (in.battery_soc_pct <= SOC_RESUME_CHARGE_PCT) batt_full_ = false;

        // Renewable power going to the load, and who supplies it. In combined mode the load is
        // shared in proportion to what each source is producing.
        const float total_ren = in.solar_kw + in.wind_kw;
        float used = 0.0f;
        if (current_ == SRC_SOLAR) {
            used = in.solar_kw < demand ? in.solar_kw : demand;
            d.load_from_solar_kw = used;
        } else if (current_ == SRC_WIND) {
            used = in.wind_kw < demand ? in.wind_kw : demand;
            d.load_from_wind_kw = used;
        } else if (current_ == SRC_SOLAR_WIND) {
            used = total_ren < demand ? total_ren : demand;
            if (total_ren > 0.0f) {
                d.load_from_solar_kw = used * (in.solar_kw / total_ren);
                d.load_from_wind_kw  = used - d.load_from_solar_kw;
            }
        }
        float surplus = total_ren - used;   // everything the load isn't using
        if (surplus < 0.0f) surplus = 0.0f;

        if (current_ == SRC_BATTERY) {
            d.battery_mode      = BATT_DISCHARGING;   // cannot charge while discharging
            d.battery_charge_kw = 0.0f;
            d.grid_export_kw    = surplus;
        } else if (surplus > MIN_SURPLUS_KW) {
            if (!batt_full_) {
                d.battery_mode      = BATT_CHARGING;
                d.battery_charge_kw = surplus < BATT_MAX_CHARGE_KW ? surplus : BATT_MAX_CHARGE_KW;
                d.grid_export_kw    = surplus - d.battery_charge_kw;
            } else {
                d.battery_mode   = BATT_FULL;
                d.grid_export_kw = surplus;
            }
        } else {
            d.battery_mode = batt_full_ ? BATT_FULL : BATT_IDLE;
        }
        return d;
    }

    Source current() const { return current_; }

private:
    static constexpr const char* REASON_INIT   = "initial selection";
    static constexpr const char* REASON_SAFETY = "safety override: source insufficient";
    static constexpr const char* REASON_SCORE  = "better suitability score";

    Source      current_        = SRC_GRID;
    bool        started_        = false;
    bool        batt_full_      = false;
    uint32_t    last_switch_ms_ = 0;
    const char* reason_         = REASON_INIT;

    // From the combined mode, go back to a single renewable as soon as one can cover demand
    // (after the dwell timer), without requiring the usual score lead.
    static bool steps_down_from_combo(Source cur, Source best) {
        return cur == SRC_SOLAR_WIND && (best == SRC_SOLAR || best == SRC_WIND);
    }

    static float headroom(float gen, float demand) {
        return clampf((gen / demand - 1.0f) / HEADROOM_FULL, 0.0f, 1.0f);
    }

    static float suitability(Source s, const SystemInputs& in, float demand) {
        const bool peak = in.is_peak_tariff;
        float cost = 0.0f, carbon = 0.0f, rel = 0.0f;
        switch (s) {
            case SRC_SOLAR:
                cost = COST_RENEWABLE; carbon = CARBON_RENEWABLE;
                rel = REL_BASE_SOLAR * (0.5f + 0.5f * headroom(in.solar_kw, demand));
                break;
            case SRC_WIND:
                cost = COST_RENEWABLE; carbon = CARBON_RENEWABLE;
                rel = REL_BASE_WIND * (0.5f + 0.5f * headroom(in.wind_kw, demand));
                break;
            case SRC_SOLAR_WIND:
                cost = COST_RENEWABLE; carbon = CARBON_RENEWABLE;
                rel = REL_BASE_HYBRID * (0.5f + 0.5f * headroom(in.solar_kw + in.wind_kw, demand));
                break;
            case SRC_BATTERY: {
                cost   = peak ? COST_BATT_PEAK : COST_BATT_OFFPEAK;
                carbon = CARBON_BATT;
                const float soc_f = clampf((in.battery_soc_pct - SOC_FLOOR_PCT) / (100.0f - SOC_FLOOR_PCT), 0.0f, 1.0f);
                rel = REL_BASE_BATTERY * (0.5f + 0.5f * soc_f);
                break;
            }
            default:
                cost   = peak ? COST_GRID_PEAK : COST_GRID_OFFPEAK;
                carbon = peak ? CARBON_GRID_PEAK : CARBON_GRID_OFFPEAK;
                rel    = REL_BASE_GRID;
                break;
        }
        return clampf(W_COST * cost + W_CARBON * carbon + W_RELIABILITY * rel, 0.0f, 1.0f);
    }
};

#endif