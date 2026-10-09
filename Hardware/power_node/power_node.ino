#include <WiFi.h>
#include <WebServer.h>
#include "config.h"
#include "sensors.h"
#include "fsm_engine.h"
#include "decision_json.h"
#include "led_outputs.h"
#include "web_ui.h"

WebServer    server(80);
WebControls  g_web;        // demand + hour from the web UI
SystemInputs g_inputs;     // latest engine inputs
Decision     g_decision;   // latest engine output
FsmEngine    fsm;

void setup() {
    Serial.begin(115200);

    // Wait up to 2 s for the USB CDC serial monitor
    unsigned long start = millis();
    while (!Serial && (millis() - start < 2000)) {
        delay(10);
    }

    Serial.println("\n[BOOT] ESP32-S3 microgrid node");

    init_leds();
    init_sensors();

    // Run the engine once BEFORE the web server starts, so /api/state never serves an empty decision.
    uint32_t now = millis();
    g_inputs   = make_system_inputs(read_potentiometers(now), g_web);
    g_decision = fsm.step(g_inputs, now);
    apply_leds(g_decision.selected);

    WiFi.mode(WIFI_AP);
    init_web_ui("ESP32_Microgrid", "12345678");

    Serial.println("[BOOT] Running. Connect to Wi-Fi ESP32_Microgrid and open the address above.\n");
}

void loop() {
    server.handleClient();

    const uint32_t now = millis();
    g_inputs   = make_system_inputs(read_potentiometers(now), g_web);
    g_decision = fsm.step(g_inputs, now);
    apply_leds(g_decision.selected);

    static unsigned long last_print = 0;
    static Source last_selected = SRC_COUNT;   // forces a print on the first pass
    if (now - last_print >= 1000 || g_decision.selected != last_selected) {
        last_print = now;
        last_selected = g_decision.selected;
        if (Serial) {
            char buf[1024];
            decision_to_json(buf, sizeof buf, g_inputs, g_decision);
            Serial.println(buf);
        }
    }

    delay(5);   // yields to Wi-Fi/TCP tasks
}
