#ifndef DECISION_JSON_H
#define DECISION_JSON_H

#include "config.h"

// Serializes the decision. The first five keys match your target schema (the numbers are
// suitability scores 0..1; selected_source can now also be "solar_wind"). "solar_wind" is a
// new score key for the combined mode; the rest is extra detail for the web UI.
inline int decision_to_json(char* buf, size_t n, const SystemInputs& in, const Decision& d) {
    #define B(x) ((x) ? "true" : "false")
    return snprintf(buf, n,
        "{\"solar\":%.2f,\"wind\":%.2f,\"grid\":%.2f,\"battery\":%.2f,"
        "\"selected_source\":\"%s\",\"solar_wind\":%.2f,\"best_source\":\"%s\","
        "\"eligible\":{\"solar\":%s,\"wind\":%s,\"grid\":%s,\"battery\":%s,\"solar_wind\":%s},"
        "\"inputs\":{\"solar_kw\":%.2f,\"wind_kw\":%.2f,\"battery_soc\":%.1f,"
        "\"demand_kw\":%.2f,\"required_kw\":%.2f,\"hour\":%u,\"peak_tariff\":%s},"
        "\"load_from_solar_kw\":%.2f,\"load_from_wind_kw\":%.2f,"
        "\"holding\":%s,\"dwell_remaining_ms\":%lu,\"switch_reason\":\"%s\","
        "\"battery_mode\":\"%s\",\"battery_charge_kw\":%.2f,\"grid_export_kw\":%.2f}",
        d.score[SRC_SOLAR], d.score[SRC_WIND], d.score[SRC_GRID], d.score[SRC_BATTERY],
        SOURCE_NAMES[d.selected], d.score[SRC_SOLAR_WIND], SOURCE_NAMES[d.best],
        B(d.eligible[SRC_SOLAR]), B(d.eligible[SRC_WIND]), B(d.eligible[SRC_GRID]),
        B(d.eligible[SRC_BATTERY]), B(d.eligible[SRC_SOLAR_WIND]),
        in.solar_kw, in.wind_kw, in.battery_soc_pct,
        in.demand_kw, d.required_kw, (unsigned)in.hour, B(in.is_peak_tariff),
        d.load_from_solar_kw, d.load_from_wind_kw,
        B(d.holding), (unsigned long)d.dwell_remaining_ms, d.reason,
        BATTERY_MODE_NAMES[d.battery_mode], d.battery_charge_kw, d.grid_export_kw);
    #undef B
}

#endif