#ifndef LED_OUTPUTS_H
#define LED_OUTPUTS_H

#include "config.h"

// Four LEDs: solar, wind, battery, grid. Solar+Wind mode lights the solar AND wind LEDs.
enum { LED_SOLAR = 0, LED_WIND = 1, LED_BATTERY = 2, LED_GRID = 3, LED_COUNT = 4 };
static const uint8_t LED_PINS[LED_COUNT] = { PIN_LED_SOLAR, PIN_LED_WIND, PIN_LED_BATTERY, PIN_LED_GRID };

inline void init_leds() {
    for (int i = 0; i < LED_COUNT; i++) {
        pinMode(LED_PINS[i], OUTPUT);
        digitalWrite(LED_PINS[i], LOW);
    }
}

inline void apply_leds(Source selected) {
    const bool on[LED_COUNT] = {
        selected == SRC_SOLAR || selected == SRC_SOLAR_WIND,
        selected == SRC_WIND  || selected == SRC_SOLAR_WIND,
        selected == SRC_BATTERY,
        selected == SRC_GRID
    };
    for (int i = 0; i < LED_COUNT; i++) {
        digitalWrite(LED_PINS[i], on[i] ? HIGH : LOW);
    }
}

#endif