import { useState, useRef, useEffect } from 'react'
import Sidebar from './Sidebar.jsx'
import ParametersPanel from './ParametersPanel.jsx'
import Outputs from './Outputs.jsx'
import { interpretVision, generateCityModel, missingParams } from '../lib/interpreter.js'

const PARAM_LABELS = {
  location: 'Location',
  land_area_km2: 'Land Area (km²)',
  population: 'Population',
}

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

export default function Builder({ onHome, onMyCities }) {
  const [params, setParams] = useState({ location: '', land_area_km2: '', population: '' })
  const [messages, setMessages] = useState([])
  const [started, setStarted] = useState(false)
  const [input, setInput] = useState('')
  const [requirements, setRequirements] = useState(null)
  const [cityModel, setCityModel] = useState(null)
  const [busy, setBusy] = useState(false)
  const [planBusy, setPlanBusy] = useState(false)
  const [planError, setPlanError] = useState(null)
  const [sidebarOpen, setSidebarOpen] = useState(true)
  const endRef = useRef(null)

  useEffect(() => {
    if (started) endRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, started])

  const addMessage = (role, text) => setMessages((prev) => [...prev, { role, text }])

  async function handleSend(e) {
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
        `Before I can build the requirements, I need these parameters: ${names}. ` +
          'Please fill them in on the right and send your vision again.'
      )
      return
    }

    setBusy(true)
    addMessage('bot', 'Interpreting your vision…')
    try {
      const { schema, notes } = await interpretVision(text, params)
      setRequirements(schema)
      setCityModel(null)
      setPlanError(null)
      setMessages((prev) => [
        ...prev.slice(0, -1),
        {
          role: 'bot',
          text:
            'Got it — I interpreted your vision into a formal requirements schema. ' +
            notes.join(' ') +
            ' Open the City Model tab on the right to generate the full 3D-ready model.',
        },
      ])
    } finally {
      setBusy(false)
    }
  }

  async function handleGenerateModel() {
    if (!requirements || planBusy) return
    setPlanBusy(true)
    setPlanError(null)
    addMessage('bot', 'Generating the full 3D city model (blocks, roads, water, greenery, windmills)…')
    try {
      const { cityModel, modelName } = await generateCityModel(requirements, params)
      setCityModel(cityModel)
      setMessages((prev) => [
        ...prev.slice(0, -1),
        {
          role: 'bot',
          text:
            `Done — ${modelName} generated a renderable city model with ` +
            `${cityModel.blocks?.length ?? 0} building blocks, ` +
            `${cityModel.green_areas?.length ?? 0} green areas, ` +
            `${cityModel.water_bodies?.length ?? 0} water bodies and ` +
            `${cityModel.windmills?.length ?? 0} windmills. ` +
            'It validates against your schema. Download it or view it in 3D from the City Model tab.',
        },
      ])
    } catch (err) {
      setPlanError(String(err.message || err))
      setMessages((prev) => [...prev.slice(0, -1), { role: 'bot', text: `Couldn't generate the model: ${err.message || err}` }])
    } finally {
      setPlanBusy(false)
    }
  }

  const missing = missingParams(params)

  return (
    <div className="studio">
      <Sidebar onExit={onHome} onMyCities={onMyCities} collapsed={!sidebarOpen} />

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

            {/* Working indicator — shown while AI is processing */}
            {(busy || planBusy) && (
              <div className="builder__working">
                <span className="builder__working-dot" />
                <span className="builder__working-dot" />
                <span className="builder__working-dot" />
                <span className="builder__working-label">
                  {planBusy ? 'Generating city model…' : 'Interpreting your vision…'}
                </span>
              </div>
            )}

            {/* Planning complete banner */}
            {cityModel && !planBusy && (
              <div className="builder__complete">
                <span className="builder__complete-icon">✓</span>
                <div className="builder__complete-text">
                  <strong>Planning complete</strong>
                  <span>Your 3D city model is ready to render.</span>
                </div>
                <button
                  className="builder__complete-cta"
                  onClick={onMyCities}
                >
                  Go to My Cities →
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
        <Outputs
          requirements={requirements}
          cityModel={cityModel}
          onGenerateModel={handleGenerateModel}
          onView={onMyCities}
          planBusy={planBusy}
          planError={planError}
        />
      </aside>
    </div>
  )
}
