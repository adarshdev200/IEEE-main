import { useState, useRef, useEffect } from 'react'
import Sidebar from './Sidebar.jsx'
import ParametersPanel from './ParametersPanel.jsx'
import { missingParams } from '../lib/interpreter.js'

const PARAM_LABELS = {
  location: 'Location',
  land_area_km2: 'Land Area (km²)',
  population: 'Population',
}

// How long the simulated "generation" runs before the Generate Model button appears.
const GENERATION_DELAY_MS = 2500

const EXAMPLES = [
  'A highly walkable coastal city for 100k people with strong public transport and solar + wind energy.',
  'A compact, car-free riverside city powered by renewables with parks in every neighborhood.',
  'A dense high-rise waterfront metropolis with a marina, green rooftops and offshore wind turbines.',
]

function BotAvatar() {
  return (
    <span className="cmsg__avatar">
      <img src="/logo-mark.png" alt="CEWNity Bot" className="cmsg__avatar-img" />
    </span>
  )
}
const Arrow = () => (
  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden>
    <path d="M5 12h14M13 6l6 6-6 6" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
  </svg>
)

export default function Builder({ onHome, onMyCities, onManageCity, onCityGenerated }) {
  const [params, setParams] = useState({ location: '', land_area_km2: '', population: '', rural: false })
  const [messages, setMessages] = useState([])
  const [started, setStarted] = useState(false)
  const [input, setInput] = useState('')
  const [busy, setBusy] = useState(false)
  const [generating, setGenerating] = useState(false)
  const [modelReady, setModelReady] = useState(false)
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const endRef = useRef(null)
  const timerRef = useRef(null)

  useEffect(() => {
    if (started) endRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, started])

  // Clean up the pending timer if the component unmounts mid-generation.
  useEffect(() => () => clearTimeout(timerRef.current), [])

  const addMessage = (role, text) => setMessages((prev) => [...prev, { role, text }])

  function handleSend(e) {
    e?.preventDefault?.()
    const text = input.trim()
    if (!text || busy) return
    if (!started) setStarted(true)
    setInput('')
    addMessage('user', text)

    const missing = missingParams(params)
    if (missing.length) {
      const names = missing.map((k) => PARAM_LABELS[k]).join(', ')
      addMessage(
        'bot',
        `Before I can build your city, I need these parameters: ${names}. ` +
          'Please fill them in on the right and send your vision again.'
      )
      return
    }

    // Simulated generation — the frontend intentionally does not call the
    // interpreter or the city planner here. After a short delay a Generate
    // Model button appears that opens the prebuilt city in My Cities.
    setBusy(true)
    setModelReady(false)
    setGenerating(true)
    addMessage('bot', 'Generating a model…')

    clearTimeout(timerRef.current)
    timerRef.current = setTimeout(() => {
      setGenerating(false)
      setBusy(false)
      setModelReady(true)
      // Persist the city's location + parameters so Manage City can monitor it.
      onCityGenerated?.({
        name: params.location,
        coordinates: params.coordinates,
        land_area_km2: params.land_area_km2,
        population: params.population,
        rural: !!params.rural,
      })
      setMessages((prev) => [
        ...prev.slice(0, -1),
        { role: 'bot', text: 'Your sustainable city model is ready. Click Generate Model to open it in My Cities, or check Manage City for its live disaster outlook.' },
      ])
    }, GENERATION_DELAY_MS)
  }

  const missing = missingParams(params)

  return (
    <div className="studio">
      <Sidebar onExit={onHome} onMyCities={onMyCities} onManageCity={onManageCity} active="Home" collapsed={!sidebarOpen} />

      <main className="studio__chat">
        <button
          className="hamburger"
          onClick={() => setSidebarOpen((o) => !o)}
          aria-label={sidebarOpen ? 'Collapse sidebar' : 'Open sidebar'}
          aria-expanded={sidebarOpen}
        >
          <span />
          <span />
          <span />
        </button>

        <div className={`conv${started ? ' is-started' : ''}`}>
          <div className="conv__scroll">
            {messages.map((m, i) => (
              <div key={i} className={`cmsg cmsg--${m.role}`}>
                {m.role === 'bot' && <BotAvatar />}
                <div className="cmsg__text">{m.text}</div>
              </div>
            ))}
            <div ref={endRef} />
          </div>

          <div className="composer-wrap">
            <div className="conv__hero">
              <h1 className="conv__title">What are we building next?</h1>
              <p className="conv__sub">
                Set your parameters on the right, describe your vision, and I'll turn it into a
                sustainable 3D city.
              </p>
            </div>

            {/* Generating indicator — circular loader while the model is "generated" */}
            {generating && (
              <div className="builder__generating">
                <span className="builder__spinner" aria-hidden />
                <span className="builder__working-label">Generating a model…</span>
              </div>
            )}

            {/* Generate Model button — appears once generation "completes" */}
            {modelReady && !generating && (
              <div className="builder__complete">
                <span className="builder__complete-icon">✓</span>
                <div className="builder__complete-text">
                  <strong>Model ready</strong>
                  <span>Your 3D city model has been generated.</span>
                </div>
                <button className="builder__complete-cta" onClick={onMyCities}>
                  Generate Model →
                </button>
              </div>
            )}

            <form className="composer" onSubmit={handleSend}>
              <svg className="composer__spark" width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden>
                <path d="M12 3l1.6 4.8L18 9.4l-4.4 1.6L12 16l-1.6-5L6 9.4l4.4-1.6L12 3z" fill="#2f8255" />
                <path d="M19 14l.7 2 2 .7-2 .7-.7 2-.7-2-2-.7 2-.7.7-2z" fill="#7cc79e" />
              </svg>
              <textarea
                value={input}
                onChange={(e) => setInput(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault()
                    handleSend(e)
                  }
                }}
                placeholder="Describe your vision for a sustainable city…"
                rows={1}
              />
              <button type="submit" className="composer__go" disabled={!input.trim() || busy} aria-label="Send">
                <Arrow />
              </button>
            </form>

            {!started && (
              <div className="composer__examples">
                {EXAMPLES.map((ex) => (
                  <button key={ex} className="composer__chip" onClick={() => setInput(ex)}>
                    {ex}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      </main>

      <aside className="studio__right">
        <ParametersPanel params={params} setParams={setParams} missing={missing} labels={PARAM_LABELS} />
      </aside>
    </div>
  )
}
