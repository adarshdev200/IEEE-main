import { useState } from 'react'
import LocationPicker from './LocationPicker.jsx'

export const ADVANCED = [
  { key: 'green_spaces', label: 'Green Spaces', options: ['Low', 'Balanced', 'Extensive'], hint: 'Parks, urban forests, green roofs and gardens.' },
  { key: 'public_transport', label: 'Public Transportation', options: ['Car-focused', 'Balanced', 'Transit-first'], hint: 'Roads, metro, buses, cycling and walkability.' },
  { key: 'building_density', label: 'Building Density', options: ['Low-rise', 'Mixed-use', 'High-rise'], hint: 'Determines how buildings are distributed.' },
  { key: 'water_management', label: 'Water Management', options: ['Basic', 'Efficient', 'Advanced'], hint: 'Rainwater harvesting, recycling, lakes and flood management.' },
  { key: 'waste_management', label: 'Waste Management', options: ['Standard', 'Recycling-focused', 'Circular economy'] },
  { key: 'carbon_footprint', label: 'Carbon Footprint', options: ['Low-carbon', 'Net-zero target', 'Carbon-negative target'] },
  { key: 'climate_resilience', label: 'Climate Resilience', options: ['Standard', 'Heat-resilient', 'Flood-resilient', 'Multi-hazard resilient'] },
  { key: 'development_budget', label: 'Development Budget', options: ['Economical', 'Moderate', 'Premium', 'Unlimited'], hint: 'Overall spend envelope for the build.' },
]

export default function ParametersPanel({ params, setParams, missing, labels }) {
  const [showAdv, setShowAdv] = useState(false)
  const [pickOpen, setPickOpen] = useState(false)

  function update(key, value) {
    setParams((prev) => ({ ...prev, [key]: value }))
  }

  function onPickLocation(address, coords) {
    setParams((prev) => ({
      ...prev,
      location: address,
      coordinates: coords ? `${coords.lat.toFixed(5)},${coords.lng.toFixed(5)}` : prev.coordinates,
    }))
    setPickOpen(false)
  }

  const fields = [
    { key: 'land_area_km2', type: 'number', placeholder: 'e.g. 50' },
    { key: 'population', type: 'number', placeholder: 'e.g. 100000' },
  ]

  const advSet = ADVANCED.filter((a) => params[a.key]).length

  return (
    <div className="params">
      <div className="params__head">
        <h2>Required Parameters</h2>
        <span className={`params__status ${missing.length ? 'is-warn' : 'is-ok'}`}>
          {missing.length ? `${missing.length} missing` : 'Complete'}
        </span>
      </div>

      <div className="field">
        <span className="field__label">
          {labels.location}
          {missing.includes('location') && <em className="field__req">required</em>}
        </span>
        <button type="button" className="field__location" onClick={() => setPickOpen(true)}>
          <svg width="17" height="17" viewBox="0 0 24 24" fill="none" aria-hidden>
            <path d="M12 21s7-6.2 7-11a7 7 0 10-14 0c0 4.8 7 11 7 11z" stroke="currentColor" strokeWidth="1.6" fill="none" />
            <circle cx="12" cy="10" r="2.4" stroke="currentColor" strokeWidth="1.6" fill="none" />
          </svg>
          <span className={params.location ? '' : 'is-placeholder'}>
            {params.location || 'Select location on the map'}
          </span>
        </button>
      </div>

      {fields.map(({ key, type, placeholder }) => (
        <label key={key} className="field">
          <span className="field__label">
            {labels[key]}
            {missing.includes(key) && <em className="field__req">required</em>}
          </span>
          <input
            type={type}
            min={type === 'number' ? 1 : undefined}
            value={params[key]}
            placeholder={placeholder}
            onChange={(e) => update(key, e.target.value)}
          />
        </label>
      ))}

      <label className="field field--check">
        <input
          type="checkbox"
          className="field__checkbox"
          checked={!!params.rural}
          onChange={(e) => update('rural', e.target.checked)}
        />
        <span className="field__check-text">
          <span className="field__label">Rural / agricultural area</span>
          <span className="field__check-hint">
            Enables a Farmer Resilience panel in Manage City — soil moisture, irrigation,
            field-worker heat windows and rainwater harvesting for this location.
          </span>
        </span>
      </label>

      <button
        type="button"
        className={`params__advtoggle${showAdv ? ' is-open' : ''}`}
        onClick={() => setShowAdv((s) => !s)}
        aria-expanded={showAdv}
      >
        <span>
          Advanced parameters
          {advSet > 0 && <em className="params__advcount">{advSet}</em>}
        </span>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden>
          <path d="M6 9l6 6 6-6" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>

      {showAdv && (
        <div className="params__adv">
          {ADVANCED.map((a) => (
            <label key={a.key} className="adv-field">
              <span className="adv-field__label">{a.label}</span>
              {a.hint && <span className="adv-field__hint">{a.hint}</span>}
              <select value={params[a.key] || ''} onChange={(e) => update(a.key, e.target.value)}>
                <option value="">No preference</option>
                {a.options.map((o) => (
                  <option key={o} value={o}>
                    {o}
                  </option>
                ))}
              </select>
            </label>
          ))}
        </div>
      )}

      {pickOpen && (
        <LocationPicker
          initial={params.location}
          onSelect={onPickLocation}
          onClose={() => setPickOpen(false)}
        />
      )}
    </div>
  )
}
