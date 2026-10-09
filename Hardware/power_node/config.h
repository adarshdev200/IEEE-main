#ifndef CONFIG_H
#define CONFIG_H

// Builds on the ESP32 (Arduino) and on a PC (for host testing of the decision engine).
#ifdef ARDUINO
  #include <Arduino.h>
#else
  #include <stdint.h>
  #include <stddef.h>
  #include <stdio.h>
#endif

// ======================= HARDWARE PINS (ESP32-S3) =======================
// Pots stay on ADC1 only (ADC2 conflicts with Wi-Fi). All pins below are on the LEFT header
// of an ESP32-S3-DevKitC-1 (the side with 3V3, RST, GPIO4..GPIO14, 5V, GND) so the whole
// build fits on one side. Check your board's pinout if it is a different S3 board.
#define PIN_POT_SOLAR    5   // GPIO5 (ADC1_CH4) - solar output  0..SOLAR_RATED_KW
#define PIN_POT_BATTERY  6   // GPIO6 (ADC1_CH5) - battery SOC   0..100 %
#define PIN_POT_WIND     4   // GPIO4 (ADC1_CH3) - wind output   0..WIND_RATED_KW

// One LED per source, each through ~330 ohm to GND. Solar+Wind mode lights BOTH the solar and wind LEDs.
// GPIO11-14 are free on the S3 (not flash/PSRAM, not strapping, not USB/UART0).
#define PIN_LED_SOLAR    7
#define PIN_LED_WIND     8
#define PIN_LED_BATTERY  9
#define PIN_LED_GRID     10

// ======================= SENSOR SETTINGS =======================
// S3 ADC at 11 dB saturates near 3.1 V, so treat 3100 mV as pot full scale.
constexpr float    ADC_FULLSCALE_MV = 3100.0f;
constexpr uint32_t SENSOR_SAMPLE_MS = 10;     // fixed sample period for the EMA
constexpr float    EMA_ALPHA        = 0.15f;

// ======================= SYSTEM RATINGS (prototype) =======================
constexpr float SOLAR_RATED_KW         = 5.0f;
constexpr float WIND_RATED_KW          = 5.0f;
constexpr float BATT_MAX_DISCHARGE_KW  = 5.0f;
constexpr float BATT_MAX_CHARGE_KW     = 3.0f;
constexpr float DEMAND_MAX_KW          = 6.0f;   // web UI clamp (normal use <= ~3 kW)
constexpr float DEMAND_FLOOR_KW        = 0.05f;  // a dead source must not count as "sufficient" at 0 kW demand

// ======================= DECISION RULES =======================
// A source may take over only if it covers demand * (1 + ENGAGE_MARGIN).
// The currently selected source may keep serving down to demand * (1 + HOLD_MARGIN).
constexpr float ENGAGE_MARGIN = 0.10f;
constexpr float HOLD_MARGIN   = 0.00f;

// Battery state of charge limits (%), with hysteresis.
constexpr float SOC_FLOOR_PCT         = 20.0f;  // below this the battery must stop discharging (safety, bypasses dwell)
constexpr float SOC_ENGAGE_PCT        = 25.0f;  // must be above this to START discharging
constexpr float SOC_FULL_PCT          = 95.0f;  // stop charging, export surplus instead
constexpr float SOC_RESUME_CHARGE_PCT = 90.0f;  // resume charging below this

constexpr uint32_t DWELL_MS            = 3000;  // minimum time between non-safety switches
constexpr float    SCORE_SWITCH_MARGIN = 0.05f; // challenger must beat current score by this much
constexpr float    MIN_SURPLUS_KW      = 0.05f;

constexpr uint8_t PEAK_START_HOUR = 17;
constexpr uint8_t PEAK_END_HOUR   = 21;         // inclusive

// ======================= SUITABILITY SCORE (0..1, higher = better) =======================
// PLACEHOLDER numbers - tune them. Weights must sum to 1.
constexpr float W_COST        = 0.40f;
constexpr float W_CARBON      = 0.30f;
constexpr float W_RELIABILITY = 0.30f;

constexpr float COST_RENEWABLE     = 1.00f;
constexpr float COST_BATT_PEAK     = 0.70f;  // stored energy is valuable at peak
constexpr float COST_BATT_OFFPEAK  = 0.10f;  // preserve battery for peak
constexpr float COST_GRID_PEAK     = 0.05f;
constexpr float COST_GRID_OFFPEAK  = 0.60f;

constexpr float CARBON_RENEWABLE    = 1.00f;
constexpr float CARBON_BATT         = 0.85f;
constexpr float CARBON_GRID_PEAK    = 0.15f;
constexpr float CARBON_GRID_OFFPEAK = 0.30f;

constexpr float REL_BASE_SOLAR   = 0.75f;
constexpr float REL_BASE_WIND    = 0.65f;
constexpr float REL_BASE_BATTERY = 0.85f;
constexpr float REL_BASE_GRID    = 0.95f;
constexpr float REL_BASE_HYBRID  = 0.70f;  // solar+wind together (average of the two)
constexpr float HEADROOM_FULL    = 0.50f;  // 50% above demand = full reliability credit

// Solar+Wind is the fallback tier for renewables. While a single renewable already covers demand,
// the combo is shown this much BELOW the best single one, and the engine steps back down to the
// single source (after the dwell) without needing the usual score lead.
constexpr float HYBRID_BELOW_SOLO_GAP = 0.01f;

// ======================= TYPES =======================
// SRC_SOLAR_WIND = solar and wind supplying the load together (used when neither covers demand alone).
enum Source : uint8_t { SRC_SOLAR = 0, SRC_WIND = 1, SRC_BATTERY = 2, SRC_GRID = 3, SRC_SOLAR_WIND = 4, SRC_COUNT = 5 };

static const char* const SOURCE_NAMES[SRC_COUNT] = { "solar", "wind", "battery", "grid", "solar_wind" };

enum BatteryMode : uint8_t { BATT_IDLE = 0, BATT_CHARGING, BATT_DISCHARGING, BATT_FULL };

static const char* const BATTERY_MODE_NAMES[4] = { "idle", "charging", "discharging", "full" };

struct PotInputs {
    float solar_kw;
    float wind_kw;
    float battery_soc_pct;
};

// Values entered in the web UI.
struct WebControls {
    float   demand_kw = 2.0f;
    uint8_t hour      = 14;
};

struct SystemInputs {
    float   solar_kw;
    float   wind_kw;
    float   battery_soc_pct;
    float   demand_kw;
    uint8_t hour;
    bool    is_peak_tariff;
};

struct Decision {
    Source      selected;
    Source      best;                 // best-scoring eligible source right now
    float       score[SRC_COUNT];     // suitability 0..1 (0 when not eligible)
    bool        eligible[SRC_COUNT];  // can this source cover demand + margin
    float       required_kw;          // demand + engage margin
    float       load_from_solar_kw;   // how much of the load solar is serving (0 if solar not in use)
    float       load_from_wind_kw;    // how much of the load wind is serving (0 if wind not in use)
    bool        holding;              // a better source exists but dwell timer is holding it back
    uint32_t    dwell_remaining_ms;
    const char* reason;               // why the last switch happened
    BatteryMode battery_mode;
    float       battery_charge_kw;
    float       grid_export_kw;
};

static inline float clampf(float v, float lo, float hi) {
    return v < lo ? lo : (v > hi ? hi : v);
}

#endif