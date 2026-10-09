// Host-side test for the decision engine. NOT part of the Arduino sketch - keep it outside power_node/.
// Build & run:  g++ -std=c++11 -Wall -Wextra -o fsm_test fsm_host_test.cpp && ./fsm_test
#include <assert.h>
#include <stdio.h>
#include <string.h>
#include "power_node/fsm_engine.h"
#include "power_node/decision_json.h"

static SystemInputs mk(float solar, float wind, float soc, float demand, uint8_t hour) {
    PotInputs p = { solar, wind, soc };
    WebControls w; w.demand_kw = demand; w.hour = hour;
    return make_system_inputs(p, w);
}

// Step the engine every 10 ms for dur_ms; returns the last decision.
static Decision run(FsmEngine& f, const SystemInputs& in, uint32_t& t, uint32_t dur_ms) {
    Decision d = Decision();
    for (uint32_t e = 0; e < dur_ms; e += 10) { t += 10; d = f.step(in, t); }
    return d;
}

#define CHECK(cond, msg) do { if (!(cond)) { printf("FAIL: %s\n", msg); return 1; } else printf("PASS: %s\n", msg); } while (0)

int main() {
    uint32_t t = 1000;
    Decision d;
    char json[1024];

    { // 1. high sun, moderate demand -> solar
        FsmEngine f; d = run(f, mk(4.5f, 0.0f, 50, 3.0f, 14), t, 100);
        CHECK(d.selected == SRC_SOLAR, "sunny + sufficient solar -> solar");
        int n = decision_to_json(json, sizeof json, mk(4.5f, 0.0f, 50, 3.0f, 14), d);
        assert(n > 0 && n < (int)sizeof json);
        printf("      %s\n", json);
    }
    { // 2. cloudy, strong wind -> wind
        FsmEngine f; d = run(f, mk(0.5f, 4.5f, 50, 3.0f, 14), t, 100);
        CHECK(d.selected == SRC_WIND, "cloudy + strong wind -> wind");
    }
    { // 3. low renewables, off-peak, healthy battery -> grid (battery preserved), surplus charges battery
        FsmEngine f; d = run(f, mk(1.0f, 1.0f, 80, 3.0f, 10), t, 100);
        CHECK(d.selected == SRC_GRID, "low renewables off-peak -> grid (battery preserved)");
        CHECK(d.battery_mode == BATT_CHARGING && d.battery_charge_kw > 1.9f, "renewable surplus charges battery");
    }
    { // 4. same at peak -> battery
        FsmEngine f; d = run(f, mk(1.0f, 1.0f, 80, 3.0f, 18), t, 100);
        CHECK(d.selected == SRC_BATTERY, "low renewables at peak, good SOC -> battery");
    }
    { // 5. peak but battery low -> grid
        FsmEngine f; d = run(f, mk(1.0f, 1.0f, 15, 3.0f, 18), t, 100);
        CHECK(d.selected == SRC_GRID, "peak, battery below floor -> grid");
    }
    { // 6. demand above any single source -> grid
        FsmEngine f; d = run(f, mk(3.0f, 3.0f, 50, 5.5f, 12), t, 100);
        CHECK(d.selected == SRC_GRID, "demand above every single source -> grid");
    }
    { // 7. margin + hysteresis
        FsmEngine f; run(f, mk(0.0f, 0.0f, 10, 3.0f, 12), t, 100);          // starts on grid
        d = run(f, mk(3.2f, 0.0f, 10, 3.0f, 12), t, 4000);                  // 3.2 < 3.3 engage threshold
        CHECK(d.selected == SRC_GRID, "solar at 107% of demand does not engage (needs 110%)");
        d = run(f, mk(3.4f, 0.0f, 10, 3.0f, 12), t, 4000);
        CHECK(d.selected == SRC_SOLAR, "solar at 113% engages");
        d = run(f, mk(3.1f, 0.0f, 10, 3.0f, 12), t, 4000);
        CHECK(d.selected == SRC_SOLAR, "solar holds at 103% (hysteresis band)");
        d = run(f, mk(2.9f, 0.0f, 10, 3.0f, 12), t, 10);
        CHECK(d.selected == SRC_GRID, "solar below demand -> immediate safety switch (bypasses dwell)");
        CHECK(strstr(d.reason, "safety") != NULL, "reason reports safety override");
    }
    { // 8. dwell timer delays voluntary switch
        FsmEngine f; run(f, mk(0.0f, 0.0f, 10, 3.0f, 12), t, 100);
        d = run(f, mk(4.5f, 0.0f, 10, 3.0f, 12), t, 1000);
        // fresh engine picked grid at start of this block's predecessor; elapsed since switch is ~1.1 s
        CHECK(d.selected == SRC_GRID && d.holding, "better source held back by dwell timer");
        CHECK(d.dwell_remaining_ms > 0 && d.dwell_remaining_ms <= DWELL_MS, "dwell countdown reported");
        d = run(f, mk(4.5f, 0.0f, 10, 3.0f, 12), t, 2500);
        CHECK(d.selected == SRC_SOLAR, "switch happens after dwell expires");
    }
    { // 9. battery full -> export
        FsmEngine f; d = run(f, mk(4.5f, 0.0f, 97, 3.0f, 12), t, 100);
        CHECK(d.selected == SRC_SOLAR && d.battery_mode == BATT_FULL && d.grid_export_kw > 1.4f,
              "battery full -> surplus exported to grid");
    }
    { // 10. battery hits SOC floor while selected -> immediate switch
        FsmEngine f; run(f, mk(0.0f, 0.0f, 80, 3.0f, 18), t, 100);
        run(f, mk(0.0f, 0.0f, 80, 3.0f, 18), t, 100);
        d = run(f, mk(0.0f, 0.0f, 80, 3.0f, 18), t, 100);
        CHECK(d.selected == SRC_BATTERY, "battery selected at peak");
        d = run(f, mk(0.0f, 0.0f, 19, 3.0f, 18), t, 10);
        CHECK(d.selected == SRC_GRID, "SOC below floor -> immediate switch to grid");
    }
    { // 11. noisy input around a threshold: voluntary switches never closer than the dwell time
        FsmEngine f; uint32_t seed = 12345; Source prev = SRC_GRID; uint32_t last_sw = 0;
        bool dwell_ok = true; int switches = 0;
        for (uint32_t i = 0; i < 6000; i++) {           // 60 s
            t += 10;
            seed = seed * 1664525u + 1013904223u;
            float noise = ((seed >> 16) & 0x7FFF) / 32767.0f - 0.5f;   // -0.5..+0.5
            d = f.step(mk(3.2f + 0.8f * noise, 0.0f, 10, 3.0f, 12), t);
            if (d.selected != prev) {
                switches++;
                if (strstr(d.reason, "safety") == NULL && last_sw != 0 && (t - last_sw) < DWELL_MS) dwell_ok = false;
                prev = d.selected; last_sw = t;
            }
        }
        printf("      noisy run: %d switches in 60 s\n", switches);
        CHECK(dwell_ok, "noise never causes a voluntary switch inside the dwell window");
    }
    { // 12. neither renewable covers demand alone, together they do -> solar+wind
        FsmEngine f; d = run(f, mk(3.0f, 3.0f, 50, 4.0f, 14), t, 100);
        CHECK(!d.eligible[SRC_SOLAR] && !d.eligible[SRC_WIND], "neither renewable covers 4 kW alone");
        CHECK(d.selected == SRC_SOLAR_WIND, "both renewables together -> solar+wind");
        CHECK(d.load_from_solar_kw > 1.99f && d.load_from_solar_kw < 2.01f &&
              d.load_from_wind_kw > 1.99f && d.load_from_wind_kw < 2.01f, "load split in proportion to output (2 kW + 2 kW)");
        CHECK(d.battery_mode == BATT_CHARGING && d.battery_charge_kw > 1.99f && d.battery_charge_kw < 2.01f,
              "combined surplus (6 - 4 = 2 kW) charges battery");
        int n = decision_to_json(json, sizeof json, mk(3.0f, 3.0f, 50, 4.0f, 14), d);
        assert(n > 0 && n < (int)sizeof json);
        printf("      %s (%d chars)\n", json, n);
    }
    { // 13. uneven split
        FsmEngine f; d = run(f, mk(4.0f, 1.0f, 50, 4.5f, 14), t, 100);   // 5.0 total >= 4.95 needed
        CHECK(d.selected == SRC_SOLAR_WIND, "4 kW solar + 1 kW wind covers 4.5 kW demand together");
        CHECK(d.load_from_solar_kw > 3.5f && d.load_from_solar_kw < 3.7f, "uneven split: solar carries ~3.6 kW");
    }
    { // 14. a single renewable is preferred over the combo when it can cover demand
        FsmEngine f; d = run(f, mk(4.0f, 4.0f, 50, 3.0f, 14), t, 100);
        CHECK(d.selected == SRC_SOLAR, "single renewable sufficient -> solo, not combo");
        CHECK(d.score[SRC_SOLAR_WIND] < d.score[SRC_SOLAR], "combo scored below the solo source");
    }
    { // 15. combo -> solo step-down goes through the dwell timer
        FsmEngine f; run(f, mk(3.0f, 3.0f, 50, 4.0f, 14), t, 100);        // combo
        d = run(f, mk(4.6f, 3.0f, 50, 4.0f, 14), t, 100);                 // solar alone now >= 4.4
        CHECK(d.selected == SRC_SOLAR_WIND && d.holding, "solar can cover demand alone: combo held by dwell");
        d = run(f, mk(4.6f, 3.0f, 50, 4.0f, 14), t, 3200);
        CHECK(d.selected == SRC_SOLAR, "steps down to solar after dwell");
    }
    { // 16. combined output falls below demand -> immediate switch off the combo
        FsmEngine f; run(f, mk(3.0f, 3.0f, 10, 4.0f, 14), t, 100);
        d = run(f, mk(1.5f, 1.5f, 10, 4.0f, 14), t, 10);
        CHECK(d.selected == SRC_GRID && strstr(d.reason, "safety") != NULL, "combined < demand -> immediate switch to grid");
    }
    { // 17. combo hysteresis: engage at 110%, hold down to 100% of demand
        FsmEngine f; run(f, mk(0.0f, 0.0f, 10, 4.0f, 14), t, 100);        // grid
        d = run(f, mk(2.1f, 2.1f, 10, 4.0f, 14), t, 4000);                // 4.2 < 4.4 engage
        CHECK(d.selected == SRC_GRID, "combined 105% of demand does not engage");
        d = run(f, mk(2.3f, 2.3f, 10, 4.0f, 14), t, 4000);                // 4.6 >= 4.4
        CHECK(d.selected == SRC_SOLAR_WIND, "combined 115% engages");
        d = run(f, mk(2.1f, 2.1f, 10, 4.0f, 14), t, 4000);                // 4.2 >= 4.0 hold
        CHECK(d.selected == SRC_SOLAR_WIND, "combo holds at 105% (hysteresis band)");
    }
    { // 18. at peak with full battery, a sufficient combo still beats the battery
        FsmEngine f; d = run(f, mk(3.0f, 3.0f, 100, 4.0f, 18), t, 100);
        CHECK(d.selected == SRC_SOLAR_WIND, "peak + full battery: combo of free renewables preferred");
    }
    { // 19. combined still can't cover demand -> grid
        FsmEngine f; d = run(f, mk(2.0f, 2.0f, 50, 5.5f, 12), t, 100);
        CHECK(d.selected == SRC_GRID, "even combined renewables insufficient -> grid");
    }
    printf("\nALL TESTS PASSED\n");
    return 0;
}
