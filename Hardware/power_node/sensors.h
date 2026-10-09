#ifndef SENSORS_H
#define SENSORS_H

#include "config.h"

// Three pots, all on ADC1. Sampled at a FIXED period (SENSOR_SAMPLE_MS) so the EMA time
// constant does not depend on how busy loop() is. Each sample is the mean of 8 reads,
// converted with analogReadMilliVolts() (uses the chip's ADC calibration).

static float    s_filt_solar     = 0.0f;   // filtered, normalized 0..1
static float    s_filt_wind      = 0.0f;
static float    s_filt_batt      = 0.0f;
static bool     s_filt_ready     = false;
static uint32_t s_last_sample_ms = 0;
static PotInputs s_pots          = { 0.0f, 0.0f, 0.0f };

static inline float read_pot_norm(int pin) {
    uint32_t acc = 0;
    for (int i = 0; i < 8; i++) acc += analogReadMilliVolts(pin);
    return clampf((acc / 8.0f) / ADC_FULLSCALE_MV, 0.0f, 1.0f);
}

inline void init_sensors() {
    analogReadResolution(12);
    analogSetAttenuation(ADC_11db);   // ~0..3.1 V range
    pinMode(PIN_POT_SOLAR,   INPUT);
    pinMode(PIN_POT_WIND,    INPUT);
    pinMode(PIN_POT_BATTERY, INPUT);
}

inline PotInputs read_potentiometers(uint32_t now_ms) {
    if (!s_filt_ready || (now_ms - s_last_sample_ms) >= SENSOR_SAMPLE_MS) {
        s_last_sample_ms = now_ms;

        const float a = read_pot_norm(PIN_POT_SOLAR);
        const float b = read_pot_norm(PIN_POT_WIND);
        const float c = read_pot_norm(PIN_POT_BATTERY);

        if (!s_filt_ready) {              // seed the filter so we don't ramp up from 0 at boot
            s_filt_solar = a; s_filt_wind = b; s_filt_batt = c;
            s_filt_ready = true;
        } else {
            s_filt_solar += EMA_ALPHA * (a - s_filt_solar);
            s_filt_wind  += EMA_ALPHA * (b - s_filt_wind);
            s_filt_batt  += EMA_ALPHA * (c - s_filt_batt);
        }

        s_pots.solar_kw         = s_filt_solar * SOLAR_RATED_KW;
        s_pots.wind_kw          = s_filt_wind  * WIND_RATED_KW;
        s_pots.battery_soc_pct  = s_filt_batt  * 100.0f;
    }
    return s_pots;
}

#endif