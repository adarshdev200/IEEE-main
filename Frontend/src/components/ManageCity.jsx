import { useEffect, useState, useCallback } from 'react'
import Sidebar from './Sidebar.jsx'
import { parseCoords, fetchAll, assessRisks, fetchAgri, assessAgri } from '../lib/disaster.js'

const WEATHER_CODES = {
  0: 'Clear sky', 1: 'Mainly clear', 2: 'Partly cloudy', 3: 'Overcast',
  45: 'Fog', 48: 'Rime fog', 51: 'Light drizzle', 53: 'Drizzle', 55: 'Heavy drizzle',
  61: 'Light rain', 63: 'Rain', 65: 'Heavy rain', 71: 'Light snow', 73: 'Snow', 75: 'Heavy snow',
  80: 'Rain showers', 81: 'Rain showers', 82: 'Violent rain showers',
  95: 'Thunderstorm', 96: 'Thunderstorm + hail', 99: 'Severe thunderstorm',
}

const HAZARD_ICON = {
  flood: '🌊', drought: '🌵', heat: '🔥', wildfire: '🔥', storm: '🌪️', earthquake: '🫨',
}

function timeAgo(ts) {
  if (!ts) return ''
  const d = typeof ts === 'number' ? ts : Date.parse(ts)
  if (!Number.isFinite(d)) return ''
  const days = Math.round((Date.now() - d) / 864e5)
  if (days <= 0) return 'today'
  if (days === 1) return 'yesterday'
  if (days < 30) return `${days} days ago`
  return `${Math.round(days / 30)} mo ago`
}

export default function ManageCity({ city, onExit, onStudio, onMyCities }) {
  const coords = parseCoords(city?.coordinates)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [data, setData] = useState(null)
  const [assessment, setAssessment] = useState(null)
  const [agri, setAgri] = useState(null)
  const [updatedAt, setUpdatedAt] = useState(null)

  const isRural = !!city?.rural

  const load = useCallback(async () => {
    if (!coords) return
    setLoading(true)
    setError(null)
    try {
      // Core disaster feed + (rural only) the agronomy feed, fetched in parallel.
      const [result, agriRaw] = await Promise.all([
        fetchAll(coords),
        isRural ? fetchAgri(coords) : Promise.resolve(null),
      ])
      setData(result)
      setAssessment(assessRisks(result))
      setAgri(
        isRural
          ? assessAgri({ weather: result.weather, agri: agriRaw, landAreaKm2: city?.land_area_km2 })
          : null
      )
      setUpdatedAt(new Date())
    } catch (e) {
      setError(String(e.message || e))
    } finally {
      setLoading(false)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [city?.coordinates, isRural, city?.land_area_km2])

  useEffect(() => { load() }, [load])

  const cur = data?.weather?.current
  const sidebar = (
    <Sidebar
      onExit={onExit}
      onStudio={onStudio}
      onMyCities={onMyCities}
      onManageCity={() => {}}
      active="Manage City"
      collapsed={false}
    />
  )

  if (!coords) {
    return (
      <div className="manage">
        {sidebar}
        <main className="manage__main">
          <div className="manage__empty">
            <h1>Manage City</h1>
            <p>
              No city location yet. Go to the builder, set a <strong>location</strong> and parameters,
              and generate your city — its location will appear here for live disaster monitoring.
            </p>
            <button className="btn-primary" onClick={onStudio}>Go to the builder →</button>
          </div>
        </main>
      </div>
    )
  }

  return (
    <div className="manage">
      {sidebar}
      <main className="manage__main">
        <header className="manage__head">
          <div>
            <h1 className="manage__title">Manage City</h1>
            <p className="manage__loc">
              <span className="manage__pin">📍</span>
              {city.name || `${coords.lat.toFixed(3)}, ${coords.lng.toFixed(3)}`}
              <span className="manage__meta">
                {city.population ? ` · ${Number(city.population).toLocaleString()} people` : ''}
                {city.land_area_km2 ? ` · ${city.land_area_km2} km²` : ''}
              </span>
            </p>
          </div>
          <button className="manage__refresh" onClick={load} disabled={loading}>
            {loading ? 'Updating…' : '↻ Refresh'}
          </button>
        </header>

        {loading && !data && (
          <div className="manage__loading">
            <span className="builder__spinner" aria-hidden />
            <span>Fetching live weather, climate and recent disaster data…</span>
          </div>
        )}

        {error && (
          <div className="manage__error">
            Couldn’t load live data: {error}. <button onClick={load}>Retry</button>
          </div>
        )}

        {assessment && (
          <>
            {/* Overall banner */}
            <section className={`manage__overall risk-bg--${assessment.overallLevel}`}>
              <div className="manage__overall-level">
                <span className="manage__overall-dot" />
                {assessment.overallLabel} risk
              </div>
              <p className="manage__overall-sum">{assessment.summary}</p>
              {updatedAt && (
                <span className="manage__updated">Live data · updated {updatedAt.toLocaleTimeString()}</span>
              )}
            </section>

            {/* Active alerts */}
            {assessment.risks.some((r) => r.alert) && (
              <section className="manage__alerts">
                <h2 className="manage__h2">⚠ Active alerts</h2>
                {assessment.risks.filter((r) => r.alert).map((r) => (
                  <div key={r.type} className={`alert risk--${r.level}`}>
                    <div className="alert__head">
                      <span className="alert__icon">{HAZARD_ICON[r.type] || '⚠'}</span>
                      <strong>{r.label}</strong>
                      <span className={`alert__badge risk-badge--${r.level}`}>{r.levelLabel}</span>
                    </div>
                    <ul className="alert__factors">
                      {r.factors.map((f, i) => <li key={i}>{f}</li>)}
                    </ul>
                    <p className="alert__rec"><strong>Recommended:</strong> {r.recommendation}</p>
                  </div>
                ))}
              </section>
            )}

            {/* Farmer resilience — rural locations only */}
            {isRural && agri && (
              <section className="manage__section agri">
                <h2 className="manage__h2">🌾 Farmer resilience</h2>
                <p className="manage__note">
                  Agronomy advisories for this rural location — root-zone soil moisture, irrigation,
                  field-worker heat and rainwater harvesting, derived from live Open-Meteo soil
                  moisture &amp; reference-ET₀ data.
                </p>

                <div className={`agri__banner risk-bg--${agri.overallLevel}`}>
                  <span className="manage__overall-dot" />
                  <span>{agri.summary}</span>
                </div>

                {agri.stats.length > 0 && (
                  <div className="manage__cards agri__stats">
                    {agri.stats.map((s) => (
                      <Stat key={s.label} label={s.label} value={s.value} sub={s.sub} />
                    ))}
                  </div>
                )}

                <div className="manage__risks agri__advisories">
                  {agri.advisories.map((a) => (
                    <div key={a.type} className={`riskcard risk--${a.level}`}>
                      <div className="riskcard__top">
                        <span className="riskcard__icon">{a.icon}</span>
                        <span className="riskcard__label">{a.label}</span>
                        <span className={`riskcard__badge risk-badge--${a.level}`}>{a.levelLabel}</span>
                      </div>
                      <div className="riskcard__meter"><span style={{ width: `${(a.level / 3) * 100}%` }} /></div>
                      <p className="riskcard__factor">{a.factors[0]}</p>
                      <p className="alert__rec"><strong>Advice:</strong> {a.recommendation}</p>
                    </div>
                  ))}
                </div>

                <div className="agri__rainwater">
                  <span className="agri__rainwater-icon">🪣</span>
                  <div>
                    <strong>Rainwater harvesting potential</strong>
                    <p>
                      {agri.rainwater.rainMm} mm of rain forecast over the next 7 days — about{' '}
                      <strong>{agri.rainwater.litersPerHa.toLocaleString()} L per hectare</strong>
                      {agri.rainwater.totalLiters != null && (
                        <> (≈ {agri.rainwater.totalLiters.toLocaleString()} L across {city.land_area_km2} km²)</>
                      )}{' '}
                      could be captured. Ready tanks, farm ponds and check-dams before the rain arrives.
                    </p>
                  </div>
                </div>
              </section>
            )}

            {/* Current conditions */}
            {cur && (
              <section className="manage__section">
                <h2 className="manage__h2">Current conditions</h2>
                <div className="manage__cards">
                  <Stat label="Temperature" value={`${Math.round(cur.temperature_2m)}°C`} sub={cur.apparent_temperature != null ? `feels ${Math.round(cur.apparent_temperature)}°C` : ''} />
                  <Stat label="Conditions" value={WEATHER_CODES[cur.weather_code] || '—'} />
                  <Stat label="Humidity" value={`${Math.round(cur.relative_humidity_2m)}%`} />
                  <Stat label="Wind" value={`${Math.round(cur.wind_speed_10m)} km/h`} />
                  <Stat label="Precipitation" value={`${cur.precipitation ?? 0} mm`} />
                </div>
              </section>
            )}

            {/* Predicted risks (all hazards) */}
            <section className="manage__section">
              <h2 className="manage__h2">Disaster risk prediction</h2>
              <p className="manage__note">
                Early-warning assessment derived from live Open-Meteo weather &amp; flood data, NASA EONET
                events and USGS seismicity for this location.
              </p>
              <div className="manage__risks">
                {assessment.risks.map((r) => (
                  <div key={r.type} className={`riskcard risk--${r.level}`}>
                    <div className="riskcard__top">
                      <span className="riskcard__icon">{HAZARD_ICON[r.type] || '•'}</span>
                      <span className="riskcard__label">{r.label}</span>
                      <span className={`riskcard__badge risk-badge--${r.level}`}>{r.levelLabel}</span>
                    </div>
                    <div className="riskcard__meter"><span style={{ width: `${(r.level / 3) * 100}%` }} /></div>
                    <p className="riskcard__factor">{r.factors[0]}</p>
                  </div>
                ))}
              </div>
            </section>

            {/* Recent disasters nearby */}
            <section className="manage__section">
              <h2 className="manage__h2">Recent disasters & events nearby</h2>
              {(data.eonet?.length || data.quakes?.length) ? (
                <ul className="manage__events">
                  {data.eonet.slice(0, 8).map((e) => (
                    <li key={e.id} className="event">
                      <span className="event__cat">{e.category}</span>
                      <span className="event__title">
                        {e.link ? <a href={e.link} target="_blank" rel="noreferrer">{e.title}</a> : e.title}
                      </span>
                      <span className="event__meta">{e.distanceKm} km · {timeAgo(e.date)}</span>
                    </li>
                  ))}
                  {data.quakes.slice(0, 5).map((q) => (
                    <li key={q.id} className="event">
                      <span className="event__cat">Earthquake</span>
                      <span className="event__title">M{q.mag?.toFixed(1)} — {q.place}</span>
                      <span className="event__meta">{timeAgo(q.time)}</span>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="manage__note">No recent natural-event or seismic reports within range of this location.</p>
              )}
            </section>
          </>
        )}
      </main>
    </div>
  )
}

function Stat({ label, value, sub }) {
  return (
    <div className="statcard">
      <span className="statcard__value">{value}</span>
      <span className="statcard__label">{label}</span>
      {sub && <span className="statcard__sub">{sub}</span>}
    </div>
  )
}
