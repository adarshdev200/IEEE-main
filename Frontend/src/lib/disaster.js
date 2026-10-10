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
