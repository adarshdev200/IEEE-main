#ifndef WEB_UI_H
#define WEB_UI_H

#include <WiFi.h>
#include <WebServer.h>
#include "config.h"
#include "decision_json.h"

// Defined in power_node.ino
extern WebServer    server;
extern WebControls  g_web;       // values entered in the web UI (demand, hour)
extern SystemInputs g_inputs;    // latest inputs fed to the engine
extern Decision     g_decision;  // latest engine output

// The page is static; it polls /api/state twice a second and POSTs /api/set when you press Apply.
const char HTML_PAGE[] PROGMEM = R"rawliteral(
<!DOCTYPE html><html><head><meta charset="utf-8"><title>Microgrid Controller</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
  body{font-family:Arial,sans-serif;margin:16px;background:#121212;color:#eee}
  .wrap{max-width:520px;margin:auto}
  .card{background:#1e1e1e;padding:16px;border-radius:8px;margin-top:12px}
  h2{margin:0 0 10px}
  h3{margin:0 0 10px;font-size:13px;color:#aaa;font-weight:normal;text-transform:uppercase;letter-spacing:.05em}
  label{display:block;margin-top:8px;color:#aaa;font-size:13px}
  input{width:100%;box-sizing:border-box;padding:8px;margin-top:4px;background:#2a2a2a;color:#fff;border:1px solid #444;border-radius:4px}
  button{margin-top:12px;width:100%;padding:10px;background:#00e676;border:none;font-weight:bold;cursor:pointer;border-radius:4px}
  #sel{font-size:32px;font-weight:bold;text-align:center;padding:14px;border-radius:8px;background:#2a2a2a}
  #sel[data-src=solar]{background:#f9a825;color:#000}
  #sel[data-src=wind]{background:#29b6f6;color:#000}
  #sel[data-src=battery]{background:#66bb6a;color:#000}
  #sel[data-src=grid]{background:#9e9e9e;color:#000}
  #sel[data-src=solar_wind]{background:linear-gradient(90deg,#f9a825,#29b6f6);color:#000}
  .row{display:flex;align-items:center;gap:8px;margin:6px 0;padding:4px 6px;border-radius:6px;border:1px solid transparent}
  .row.sel{border-color:#00e676;background:#16261c}
  .row.best{border-color:#888}
  .nm{width:62px}
  .bar{flex:1;height:10px;background:#2a2a2a;border-radius:5px;overflow:hidden}
  .fill{height:100%;background:#00e676;width:0}
  .val{width:96px;text-align:right;font-size:13px;color:#ccc}
  .muted{color:#999;font-size:13px;margin-top:8px}
  .line{margin:4px 0;font-size:14px}
  pre{background:#1e1e1e;padding:12px;border-radius:8px;font-size:12px;overflow:auto}
</style></head>
<body><div class="wrap">
  <h2>Microgrid Controller</h2>
  <div id="sel" data-src="">--</div>
  <div id="status" class="muted">connecting...</div>

  <div class="card">
    <h3>Inputs you set here</h3>
    <label>Demand (kW), 0 - 6</label><input id="demand" type="number" step="0.1" min="0" max="6">
    <label>Hour of day (0 - 23)</label><input id="hour" type="number" step="1" min="0" max="23">
    <button onclick="applyInputs()">Apply</button>
  </div>

  <div class="card">
    <h3>Live readings (pots)</h3>
    <div class="line" id="pots"></div>
    <div class="line" id="need"></div>
  </div>

  <div class="card">
    <h3>Source suitability</h3>
    <div id="rows"></div>
  </div>

  <div class="card">
    <h3>Load and surplus</h3>
    <div class="line" id="load"></div>
    <div class="line" id="batt"></div>
  </div>

  <pre id="json"></pre>
</div>
<script>
const ORDER = ['solar','wind','solar_wind','battery','grid'];
const NAMES = {solar:'Solar', wind:'Wind', solar_wind:'Solar+Wind', battery:'Battery', grid:'Grid'};
document.getElementById('rows').innerHTML = ORDER.map(function(k){
  return '<div class="row" id="r_'+k+'"><span class="nm">'+NAMES[k]+'</span>'+
         '<div class="bar"><div class="fill" id="f_'+k+'"></div></div>'+
         '<span class="val" id="v_'+k+'"></span></div>';
}).join('');

let loaded = false;
function el(id){ return document.getElementById(id); }

function render(s){
  const i = s.inputs;
  if (!loaded){ el('demand').value = i.demand_kw.toFixed(1); el('hour').value = i.hour; loaded = true; }

  el('sel').textContent = NAMES[s.selected_source].toUpperCase();
  el('sel').setAttribute('data-src', s.selected_source);

  let st = 'Last switch: ' + s.switch_reason;
  if (s.holding){
    st = NAMES[s.best_source] + ' is better - switching in ' + (s.dwell_remaining_ms/1000).toFixed(1) + ' s (dwell timer)';
  }
  el('status').textContent = st;

  el('pots').textContent = 'Solar ' + i.solar_kw.toFixed(2) + ' kW  |  Wind ' + i.wind_kw.toFixed(2) +
                           ' kW  |  Battery ' + i.battery_soc.toFixed(0) + ' %';
  el('need').textContent = 'Demand ' + i.demand_kw.toFixed(2) + ' kW, a source needs >= ' + i.required_kw.toFixed(2) +
                           ' kW to take over  |  ' + (i.peak_tariff ? 'PEAK' : 'off-peak') + ' tariff';

  ORDER.forEach(function(k){
    const ok = s.eligible[k];
    el('f_'+k).style.width = (ok ? s[k]*100 : 0) + '%';
    el('v_'+k).textContent = ok ? s[k].toFixed(2) : 'not eligible';
    let cls = 'row';
    if (k === s.selected_source) cls += ' sel';
    else if (k === s.best_source) cls += ' best';
    el('r_'+k).className = cls;
  });

  el('load').textContent = 'Load served by: solar ' + s.load_from_solar_kw.toFixed(2) + ' kW + wind ' +
                           s.load_from_wind_kw.toFixed(2) + ' kW' +
                           (s.selected_source === 'battery' ? ' + battery' : '') +
                           (s.selected_source === 'grid' ? ' + grid import' : '');

  el('batt').textContent = 'Battery: ' + s.battery_mode + '  |  charging ' + s.battery_charge_kw.toFixed(2) +
                           ' kW  |  exporting ' + s.grid_export_kw.toFixed(2) + ' kW to grid';

  el('json').textContent = JSON.stringify({solar:s.solar, wind:s.wind, grid:s.grid, battery:s.battery,
                                           selected_source:s.selected_source}, null, 2);
}

async function poll(){
  try {
    const r = await fetch('/api/state');
    render(await r.json());
  } catch(e){
    el('status').textContent = 'connection lost - retrying...';
  }
}

async function applyInputs(){
  const body = new URLSearchParams({demand: el('demand').value, hour: el('hour').value});
  try { await fetch('/api/set', {method:'POST', body: body}); } catch(e){}
  poll();
}

setInterval(poll, 500);
poll();
</script></body></html>
)rawliteral";

inline void handle_root() {
    server.send_P(200, "text/html", HTML_PAGE);
}

inline void handle_state() {
    char buf[1024];
    decision_to_json(buf, sizeof buf, g_inputs, g_decision);
    server.send(200, "application/json", buf);
}

inline void handle_set() {
    if (server.hasArg("demand")) {
        g_web.demand_kw = clampf(server.arg("demand").toFloat(), 0.0f, DEMAND_MAX_KW);
    }
    if (server.hasArg("hour")) {
        int h = server.arg("hour").toInt();
        g_web.hour = (uint8_t)(h < 0 ? 0 : (h > 23 ? 23 : h));
    }
    handle_state();   // reply with the (previous-tick) state; the page re-polls right after
}

inline void init_web_ui(const char* ssid, const char* password) {
    WiFi.softAP(ssid, password);
    IPAddress ip = WiFi.softAPIP();
    Serial.print("Web UI: http://");
    Serial.println(ip);

    server.on("/", handle_root);
    server.on("/api/state", HTTP_GET, handle_state);
    server.on("/api/set", HTTP_POST, handle_set);
    server.begin();
}

#endif