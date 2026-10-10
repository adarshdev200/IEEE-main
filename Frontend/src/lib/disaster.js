// Disaster-prediction data layer.
//
// All sources are FREE, keyless and CORS-enabled, so everything is fetched
// client-side (works in the static build too):
//   - Open-Meteo forecast .......... weather now + 14-day forecast + 30-day history
//   - Open-Meteo Flood API ......... river discharge (flood signal)
//   - NASA EONET ................... recent natural events (wildfire/flood/storm/…)
//   - USGS earthquakes ............. recent seismic activity
//
// The "prediction" is a transparent rule-based assessment of the live data
// (an early-warning heuristic), not a machine-learning forecast.

export function parseCoords(coordinates) {
  if (!coordinates) return null
  const [lat, lng] = String(coordinates).split(',').map((s) => parseFloat(s.trim()))
  if (Number.isFinite(lat) && Number.isFinite(lng)) return { lat, lng }
  return null
}

function haversineKm(a, b) {
  const R = 6371
  const dLat = ((b.lat - a.lat) * Math.PI) / 180
  const dLng = ((b.lng - a.lng) * Math.PI) / 180
  const la1 = (a.lat * Math.PI) / 180
  const la2 = (b.lat * Math.PI) / 180
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(la1) * Math.cos(la2) * Math.sin(dLng / 2) ** 2
  return 2 * R * Math.asin(Math.sqrt(h))
}

async function getJSON(url) {
  const r = await fetch(url)
  if (!r.ok) throw new Error(`Request failed (${r.status})`)
  return r.json()
}

const sum = (a) => a.reduce((s, v) => s + (Number.isFinite(v) ? v : 0), 0)
const max = (a) => a.reduce((m, v) => (Number.isFinite(v) && v > m ? v : m), -Infinity)
const rolling = (a, n) => {
  let best = 0
  for (let i = 0; i + n <= a.length; i++) best = Math.max(best, sum(a.slice(i, i + n)))
  return best
}

// ── Fetchers ────────────────────────────────────────────────────────────────

export async function fetchWeather({ lat, lng }) {
  const url =
    `https://api.open-meteo.com/v1/forecast?latitude=${lat}&longitude=${lng}` +
    `&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,wind_speed_10m,weather_code` +
    `&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,precipitation_probability_max,wind_speed_10m_max` +
    `&timezone=auto&forecast_days=14&past_days=30`
  return getJSON(url)
}

export async function fetchFlood({ lat, lng }) {
  try {
    return await getJSON(
      `https://flood-api.open-meteo.com/v1/flood?latitude=${lat}&longitude=${lng}&daily=river_discharge&forecast_days=30`
    )
  } catch {
    return null
  }
}

export async function fetchEonet(center, radiusKm = 800) {
  let data
  try {
    data = await getJSON('https://eonet.gsfc.nasa.gov/api/v3/events?status=open&limit=120&days=120')
  } catch {
    return []
  }
  const out = []
  for (const ev of data.events || []) {
    let closest = Infinity
    for (const g of ev.geometry || []) {
      let pt = null
      if (g.type === 'Point' && Array.isArray(g.coordinates)) {
        pt = { lng: g.coordinates[0], lat: g.coordinates[1] }
      } else if (Array.isArray(g.coordinates?.[0]?.[0])) {
        const c = g.coordinates[0][0]
        pt = { lng: c[0], lat: c[1] }
      }
      if (pt && Number.isFinite(pt.lat) && Number.isFinite(pt.lng)) {
        closest = Math.min(closest, haversineKm(center, pt))
      }
    }
    if (closest <= radiusKm) {
      const last = ev.geometry?.[ev.geometry.length - 1]
      out.push({
        id: ev.id,
        title: ev.title,
        category: ev.categories?.[0]?.title || 'Event',
        categoryId: ev.categories?.[0]?.id || '',
        date: last?.date || null,
        distanceKm: Math.round(closest),
        link: ev.sources?.[0]?.url || null,
      })
    }
  }
  return out.sort((a, b) => a.distanceKm - b.distanceKm)
}

export async function fetchQuakes(center, radiusKm = 500) {
  const start = new Date(Date.now() - 30 * 864e5).toISOString().slice(0, 10)
  try {
    const d = await getJSON(
      `https://earthquake.usgs.gov/fdsnws/event/1/query?format=geojson&latitude=${center.lat}` +
        `&longitude=${center.lng}&maxradiuskm=${radiusKm}&starttime=${start}&minmagnitude=3`
    )
    return (d.features || [])
      .map((f) => ({
        id: f.id,
        mag: f.properties.mag,
        place: f.properties.place,
        time: f.properties.time,
      }))
      .sort((a, b) => b.mag - a.mag)
  } catch {
    return []
  }
}

/** Fetch everything in parallel; individual failures degrade gracefully. */
export async function fetchAll(center) {
  const [weather, flood, eonet, quakes] = await Promise.all([
    fetchWeather(center),
    fetchFlood(center),
    fetchEonet(center),
    fetchQuakes(center),
  ])
  return { weather, flood, eonet, quakes }
}

// ── Risk assessment (rule-based) ──────────────────────────────────────────────

const LEVELS = ['Low', 'Moderate', 'High', 'Severe']

function hasEonet(eonet, idFragment) {
  return eonet.some((e) => e.categoryId.includes(idFragment))
}

/**
 * Derive per-hazard risk levels (0 Low … 3 Severe) from the live datasets.
 * Returns { risks: [...], summary }.
 */
export function assessRisks({ weather, flood, eonet = [], quakes = [] }) {
  const risks = []
  const daily = weather?.daily
  const cur = weather?.current

  if (daily?.time?.length) {
    const times = daily.time
    const todayStr = new Date().toISOString().slice(0, 10)
    let split = times.findIndex((t) => t >= todayStr)
    if (split < 0) split = times.length

    const foreP = daily.precipitation_sum.slice(split)
    const pastP = daily.precipitation_sum.slice(0, split)
    const foreT = daily.temperature_2m_max.slice(split)
    const foreW = daily.wind_speed_10m_max.slice(split)

    const recentPrecip = sum(pastP) // last ~30 days (mm)
    const forecastPrecip = sum(foreP) // next ~14 days (mm)
    const precip3day = rolling(foreP, 3)
    const precipDayMax = Math.max(0, max(foreP))
    const tMax = Math.max(-50, max(foreT))
    const wMax = Math.max(0, max(foreW))
    const humidity = cur?.relative_humidity_2m ?? 50

    // Flood: heavy forecast rain, elevated river discharge, or nearby flood events.
    {
      const factors = []
      let level = 0
      if (precip3day > 150) { level = Math.max(level, 3); factors.push(`${precip3day.toFixed(0)} mm rain forecast over 3 days`) }
      else if (precip3day > 90) { level = Math.max(level, 2); factors.push(`${precip3day.toFixed(0)} mm rain forecast over 3 days`) }
      else if (precipDayMax > 40) { level = Math.max(level, 1); factors.push(`peak ${precipDayMax.toFixed(0)} mm in a day forecast`) }

      const disch = flood?.daily?.river_discharge?.filter((v) => Number.isFinite(v)) || []
      if (disch.length > 5) {
        const med = [...disch].sort((a, b) => a - b)[Math.floor(disch.length / 2)]
        const peak = max(disch)
        if (med > 0 && peak / med > 2.5) { level = Math.max(level, 3); factors.push(`river discharge peaking ${(peak / med).toFixed(1)}× its median`) }
        else if (med > 0 && peak / med > 1.6) { level = Math.max(level, 2); factors.push(`river discharge rising ${(peak / med).toFixed(1)}× its median`) }
      }
      if (hasEonet(eonet, 'floods')) { level = Math.max(level, 2); factors.push('active flood event reported nearby') }

      risks.push(mkRisk('flood', 'Flooding', level, factors,
        'Clear storm drains, pre-position pumps and sandbags, and ready evacuation routes for low-lying sectors.'))
    }

    // Drought: dry recent past + dry forecast + heat, or nearby drought events.
    {
      const factors = []
      let level = 0
      if (recentPrecip < 8 && forecastPrecip < 10) { level = 3; factors.push(`only ${recentPrecip.toFixed(0)} mm rain in the last 30 days`) }
      else if (recentPrecip < 20 && forecastPrecip < 20) { level = 2; factors.push(`${recentPrecip.toFixed(0)} mm rain in the last 30 days`) }
      else if (recentPrecip < 40) { level = 1; factors.push(`below-average recent rainfall (${recentPrecip.toFixed(0)} mm/30d)`) }
      if (tMax > 35 && level > 0) factors.push(`sustained heat up to ${tMax.toFixed(0)} °C`)
      if (hasEonet(eonet, 'drought')) { level = Math.max(level, 2); factors.push('drought event reported nearby') }

      risks.push(mkRisk('drought', 'Drought & water stress', level, factors,
        'Activate water-conservation measures, protect reservoir reserves, and prioritise greywater reuse.'))
    }

    // Heatwave: high forecast max temperatures.
    {
      const factors = []
      let level = 0
      if (tMax >= 42) { level = 3; factors.push(`peak temperature ${tMax.toFixed(0)} °C forecast`) }
      else if (tMax >= 38) { level = 2; factors.push(`peak temperature ${tMax.toFixed(0)} °C forecast`) }
      else if (tMax >= 33) { level = 1; factors.push(`warm spell up to ${tMax.toFixed(0)} °C forecast`) }
      risks.push(mkRisk('heat', 'Heatwave', level, factors,
        'Open cooling centres, issue public heat advisories, and safeguard vulnerable residents and outdoor workers.'))
    }

    // Wildfire: hot + dry + low humidity, or nearby wildfires.
    {
      const factors = []
      let level = 0
      const dry = recentPrecip < 25
      if (tMax >= 35 && humidity < 30 && dry) { level = 3; factors.push(`hot (${tMax.toFixed(0)} °C), dry air (${humidity}% RH) and parched ground`) }
      else if (tMax >= 32 && humidity < 40 && dry) { level = 2; factors.push(`hot, low humidity (${humidity}% RH) and dry conditions`) }
      else if (tMax >= 30 && dry) { level = 1; factors.push('warm and dry conditions') }
      if (hasEonet(eonet, 'wildfires')) { level = Math.max(level, 2); factors.push('active wildfire reported nearby') }
      risks.push(mkRisk('wildfire', 'Wildfire', level, factors,
        'Enforce fire bans, clear vegetation buffers around the urban edge, and stage firefighting assets.'))
    }

    // Severe storm / high wind.
    {
      const factors = []
      let level = 0
      if (wMax >= 90) { level = 3; factors.push(`wind gusts up to ${wMax.toFixed(0)} km/h forecast`) }
      else if (wMax >= 65) { level = 2; factors.push(`strong winds up to ${wMax.toFixed(0)} km/h forecast`) }
      else if (wMax >= 50) { level = 1; factors.push(`gusty winds up to ${wMax.toFixed(0)} km/h forecast`) }
      if (hasEonet(eonet, 'severeStorms')) { level = Math.max(level, 2); factors.push('severe storm system reported nearby') }
      risks.push(mkRisk('storm', 'Severe storm & wind', level, factors,
        'Secure loose structures and scaffolding, inspect the grid, and pre-stage emergency crews.'))
    }
  }

  // Earthquake: recent regional seismicity (reported, not predicted).
  {
    const factors = []
    let level = 0
    const big = quakes.filter((q) => q.mag >= 4.5)
    if (quakes.some((q) => q.mag >= 6)) { level = 3; factors.push(`M${max(quakes.map((q) => q.mag)).toFixed(1)} earthquake recorded in the region`) }
    else if (big.length) { level = 2; factors.push(`${big.length} earthquake(s) ≥ M4.5 in the last 30 days nearby`) }
    else if (quakes.length) { level = 1; factors.push(`${quakes.length} minor earthquake(s) recorded nearby recently`) }
    risks.push(mkRisk('earthquake', 'Seismic activity', level, factors,
      'Review structural codes for new sectors and verify emergency response and shelter readiness.'))
  }

  risks.sort((a, b) => b.level - a.level)
  const top = risks[0]?.level ?? 0
  return {
    risks,
    overallLevel: top,
    overallLabel: LEVELS[top],
    summary:
      top >= 2
        ? `Elevated disaster risk detected — ${risks.filter((r) => r.level >= 2).map((r) => r.label.toLowerCase()).join(', ')}.`
        : top === 1
          ? 'Generally stable with some conditions worth monitoring.'
          : 'No significant disaster risk detected from current data.',
  }
}

function mkRisk(type, label, level, factors, recommendation) {
  return {
    type,
    label,
    level,
    levelLabel: LEVELS[level],
    factors: factors.length ? factors : ['No elevated indicators in current data.'],
    recommendation,
    alert: level >= 2,
  }
}

// ──────────────────────────────────────────────────────────────────────────
//  FARMER / AGRICULTURE RESILIENCE MODULE  (rural areas only)
//
//  Additive layer for rural locations. Reuses the same free, keyless
//  Open-Meteo forecast endpoint, requesting agronomy fields the core disaster
//  engine doesn't use: root-zone soil moisture & temperature, reference
//  evapotranspiration (ET₀) and an hourly heat profile for field workers.
//  Everything here is self-contained — it does not touch the disaster engine
//  above, so the core city/disaster flow is unaffected.
// ──────────────────────────────────────────────────────────────────────────

const avg = (a) => {
  const v = a.filter((x) => Number.isFinite(x))
  return v.length ? v.reduce((s, x) => s + x, 0) / v.length : NaN
}

// Index of the forecast hour closest to "now" (hourly.time are local ISO strings).
function currentHourIndex(times = []) {
  if (!times.length) return -1
  const now = Date.now()
  let best = 0
  let bestDiff = Infinity
  for (let i = 0; i < times.length; i++) {
    const t = Date.parse(times[i])
    if (!Number.isFinite(t)) continue
    const diff = Math.abs(t - now)
    if (diff < bestDiff) { bestDiff = diff; best = i }
  }
  return best
}

/**
 * Agronomy feed: root-zone soil moisture/temperature, ET₀ and an hourly heat
 * profile. Free + keyless + CORS-enabled, like the rest of this module.
 * Returns null on failure so the UI degrades gracefully.
 */
export async function fetchAgri({ lat, lng }) {
  const url =
    `https://api.open-meteo.com/v1/forecast?latitude=${lat}&longitude=${lng}` +
    `&hourly=temperature_2m,relative_humidity_2m,apparent_temperature,` +
    `soil_moisture_9_to_27cm,soil_moisture_27_to_81cm,soil_temperature_18cm,et0_fao_evapotranspiration` +
    `&daily=temperature_2m_min,temperature_2m_max,precipitation_sum,et0_fao_evapotranspiration` +
    `&timezone=auto&forecast_days=7`
  try {
    return await getJSON(url)
  } catch {
    return null
  }
}

// Volumetric soil-moisture thresholds (m³/m³) for a generic loam root zone.
// wilting point ≈ 0.12, field capacity ≈ 0.30.
function soilMoistureBand(vwc) {
  if (!Number.isFinite(vwc)) return { level: 0, label: 'Unknown' }
  if (vwc < 0.12) return { level: 3, label: 'Very dry' }
  if (vwc < 0.18) return { level: 2, label: 'Dry' }
  if (vwc < 0.30) return { level: 0, label: 'Adequate' }
  return { level: 1, label: 'Saturated' }
}

/**
 * Derive farmer-facing advisories from the live agronomy + weather feeds.
 * `weather` is the same object fetched by fetchAll() (reused, not re-fetched);
 * `agri` is fetchAgri()'s result; `landAreaKm2` sizes the rainwater estimate.
 *
 * Returns { stats, advisories, rainwater, overallLevel, overallLabel, summary }.
 */
export function assessAgri({ weather, agri, landAreaKm2 } = {}) {
  const advisories = []
  const stats = []
  const hourly = agri?.hourly
  const daily = agri?.daily

  // ── Current soil state (root zone) ──────────────────────────────────────
  let soilBand = { level: 0, label: 'Unknown' }
  let rootVwc = NaN
  let soilTempC = NaN
  if (hourly?.time?.length) {
    const i = currentHourIndex(hourly.time)
    const shallow = hourly.soil_moisture_9_to_27cm?.[i]
    const deep = hourly.soil_moisture_27_to_81cm?.[i]
    rootVwc = avg([shallow, deep])
    soilTempC = hourly.soil_temperature_18cm?.[i]
    soilBand = soilMoistureBand(rootVwc)
    if (Number.isFinite(rootVwc)) {
      stats.push({ label: 'Root-zone moisture', value: `${Math.round(rootVwc * 100)}%`, sub: soilBand.label })
    }
    if (Number.isFinite(soilTempC)) {
      stats.push({ label: 'Soil temperature', value: `${Math.round(soilTempC)}°C`, sub: '18 cm depth' })
    }
  }

  // ── Water balance: ET₀ vs rainfall over the next 3 days ─────────────────
  let et3 = NaN
  let rain3 = NaN
  let rain7 = NaN
  if (daily?.time?.length) {
    const et = daily.et0_fao_evapotranspiration || []
    const pr = daily.precipitation_sum || []
    et3 = sum(et.slice(0, 3))
    rain3 = sum(pr.slice(0, 3))
    rain7 = sum(pr.slice(0, 7))
    if (Number.isFinite(et3)) {
      stats.push({ label: 'Crop water use (3d)', value: `${et3.toFixed(1)} mm`, sub: 'reference ET₀' })
    }
    stats.push({ label: 'Rain forecast (3d)', value: `${rain3.toFixed(0)} mm`, sub: `${rain7.toFixed(0)} mm over 7d` })
  }

  // ── Crop water-stress advisory ──────────────────────────────────────────
  {
    const factors = []
    let level = soilBand.level >= 2 ? soilBand.level : 0
    if (soilBand.level >= 2) factors.push(`root-zone soil is ${soilBand.label.toLowerCase()} (${Math.round(rootVwc * 100)}% moisture)`)
    const deficit = Number.isFinite(et3) && Number.isFinite(rain3) ? et3 - rain3 : NaN
    if (Number.isFinite(deficit)) {
      if (deficit > 15 && rain7 < 10) { level = Math.max(level, 3); factors.push(`crops will lose ~${deficit.toFixed(0)} mm more water than rain replaces over 3 days`) }
      else if (deficit > 8) { level = Math.max(level, 2); factors.push(`water deficit of ~${deficit.toFixed(0)} mm forecast over 3 days`) }
      else if (deficit > 3) { level = Math.max(level, 1); factors.push(`mild water deficit (~${deficit.toFixed(0)} mm) over 3 days`) }
    }
    advisories.push(mkAdvisory('cropwater', 'Crop water stress', '🌱', level, factors,
      level >= 2
        ? 'Soil is drying faster than rain replaces it — irrigate soon and mulch to hold moisture. Prioritise young and flowering crops.'
        : 'Soil moisture is adequate. Hold irrigation and re-check in a few days.'))
  }

  // ── Irrigation scheduling ───────────────────────────────────────────────
  let irrigationMm = 0
  {
    const factors = []
    let level = 0
    const need = Number.isFinite(et3) && Number.isFinite(rain3) ? Math.max(0, et3 - rain3) : NaN
    irrigationMm = Number.isFinite(need) ? Math.round(need) : 0
    let rec
    if (Number.isFinite(need)) {
      if (rain3 >= et3 && rain3 >= 8) {
        level = 0
        rec = `Skip irrigation — ${rain3.toFixed(0)} mm of rain over the next 3 days covers crop demand. Save water and fuel.`
        factors.push(`${rain3.toFixed(0)} mm rain forecast ≥ ${et3.toFixed(0)} mm crop demand`)
      } else if (need > 15) {
        level = 2
        rec = `Apply about ${irrigationMm} mm this week (≈ ${(irrigationMm * 10000).toLocaleString()} L per hectare), split over 2 turns at dawn/dusk to cut evaporation.`
        factors.push(`${irrigationMm} mm shortfall after accounting for forecast rain`)
      } else if (need > 3) {
        level = 1
        rec = `A light ${irrigationMm} mm top-up (≈ ${(irrigationMm * 10000).toLocaleString()} L per hectare) will keep the root zone comfortable.`
        factors.push(`${irrigationMm} mm shortfall after forecast rain`)
      } else {
        level = 0
        rec = 'No irrigation needed right now — rainfall roughly balances crop water use.'
        factors.push('forecast rain roughly matches crop water demand')
      }
    } else {
      rec = 'Irrigation guidance unavailable — agronomy feed could not be loaded.'
    }
    advisories.push(mkAdvisory('irrigation', 'Irrigation advisory', '💧', level, factors, rec))
  }

  // ── Heat stress for field workers (today's safe-work window) ────────────
  if (hourly?.time?.length) {
    const factors = []
    const times = hourly.time
    const feels = hourly.apparent_temperature || []
    const today = (times[currentHourIndex(times)] || '').slice(0, 10)
    const dangerous = []
    let peak = -Infinity
    for (let i = 0; i < times.length; i++) {
      if (times[i].slice(0, 10) !== today) continue
      const f = feels[i]
      const hr = Number(times[i].slice(11, 13))
      if (Number.isFinite(f)) {
        peak = Math.max(peak, f)
        if (f >= 32) dangerous.push(hr)
      }
    }
    let level = 0
    let windowTxt = 'No dangerous heat expected today — normal field hours are fine.'
    if (dangerous.length) {
      const from = Math.min(...dangerous)
      const to = Math.min(24, Math.max(...dangerous) + 1)
      const pad = (h) => (h >= 24 ? 'midnight' : `${String(h).padStart(2, '0')}:00`)
      if (peak >= 41) level = 3
      else if (peak >= 38) level = 2
      else level = 1
      windowTxt = `Avoid field work ${pad(from)}–${pad(to)} (feels up to ${Math.round(peak)}°C). Work before ${pad(from)} or after ${pad(to)}.`
      factors.push(`heat index peaks near ${Math.round(peak)}°C today`)
    }
    advisories.push(mkAdvisory('heatwork', 'Field-worker heat window', '🥵', level, factors,
      `${windowTxt} Carry water, rest in shade every 20–30 min, and watch for dizziness or cramps.`))
  }

  // ── Frost / cold-snap alert for crops ───────────────────────────────────
  if (daily?.temperature_2m_min?.length) {
    const factors = []
    let level = 0
    const minT = Math.min(...daily.temperature_2m_min.slice(0, 5).filter(Number.isFinite))
    if (Number.isFinite(minT)) {
      if (minT <= 0) { level = 3; factors.push(`overnight lows near ${minT.toFixed(0)}°C — hard frost likely`) }
      else if (minT <= 2) { level = 2; factors.push(`overnight lows near ${minT.toFixed(0)}°C — frost risk`) }
      else if (minT <= 5) { level = 1; factors.push(`cool nights near ${minT.toFixed(0)}°C`) }
    }
    if (level > 0) {
      advisories.push(mkAdvisory('frost', 'Frost & cold snap', '❄️', level, factors,
        'Protect sensitive crops: cover seedlings, irrigate lightly before dusk, or run smoke/row covers overnight to trap heat.'))
    }
  }

  // ── Rainwater-harvesting potential (next 7 days) ────────────────────────
  const EFF = 0.8 // collection/runoff efficiency
  const rainwater = {
    rainMm: Number.isFinite(rain7) ? Math.round(rain7) : 0,
    litersPerHa: Number.isFinite(rain7) ? Math.round(rain7 * 10000 * EFF) : 0,
    totalLiters:
      Number.isFinite(rain7) && Number(landAreaKm2) > 0
        ? Math.round(rain7 * Number(landAreaKm2) * 1e6 * EFF)
        : null,
  }

  // ── Roll-up ─────────────────────────────────────────────────────────────
  advisories.sort((a, b) => b.level - a.level)
  const top = advisories[0]?.level ?? 0
  const pressing = advisories.filter((a) => a.level >= 2)
  return {
    stats,
    advisories,
    rainwater,
    overallLevel: top,
    overallLabel: LEVELS[top],
    summary: pressing.length
      ? `Action needed for ${pressing.map((a) => a.label.toLowerCase()).join(', ')}.`
      : top === 1
        ? 'Conditions are broadly favourable — a few things worth watching.'
        : 'Favourable conditions for crops and field work right now.',
  }
}

function mkAdvisory(type, label, icon, level, factors, recommendation) {
  return {
    type,
    label,
    icon,
    level,
    levelLabel: LEVELS[level],
    factors: factors.length ? factors : ['No elevated indicators in current data.'],
    recommendation,
    alert: level >= 2,
  }
}
