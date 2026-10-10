import { useState } from 'react'
import Landing from './components/Landing.jsx'
import Login from './components/Login.jsx'
import Builder from './components/Builder.jsx'

export default function App() {
  const [view, setView] = useState('home') // 'home' | 'login' | 'builder' | 'cityview'

  if (view === 'login') {
    return <Login onSignIn={() => setView('builder')} onBack={() => setView('home')} />
  }
  if (view === 'builder') {
    return (
      <Builder
        onHome={() => setView('home')}
        onMyCities={() => setView('cityview')}
      />
    )
  }
  if (view === 'cityview') {
    return (
      <div style={{ position: 'fixed', inset: 0, background: '#000', zIndex: 9999 }}>
        <button
          onClick={() => setView('builder')}
          style={{
            position: 'absolute',
            top: 14,
            left: 14,
            zIndex: 10000,
            padding: '8px 16px',
            background: 'rgba(0,0,0,0.6)',
            color: '#fff',
            border: '1px solid rgba(255,255,255,0.25)',
            borderRadius: 8,
            cursor: 'pointer',
            fontSize: 13,
            backdropFilter: 'blur(6px)',
          }}
        >
          ← Back
        </button>
        <iframe
          src="/CityView/index.html"
          title="3D City Renderer"
          style={{ width: '100%', height: '100%', border: 'none', display: 'block' }}
          allow="fullscreen"
        />
      </div>
    )
  }
  return <Landing onStart={() => setView('login')} />
}
