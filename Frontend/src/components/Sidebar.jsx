import { useState } from 'react'

function LeafMark({ size = 28 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" aria-hidden>
      <path d="M20 4C9 4 4 9.5 4 17c0 1 .2 2 .5 3C7 14 12 11 18 10c-5 2.3-8.5 6-10.5 11 8 .5 15-4 15-14 0-1.2-.1-2.3-.3-3H20z" fill="#2f7d52" />
      <path d="M6 20C8 14 12 11 18 10" stroke="#eaf5ee" strokeWidth="1.4" strokeLinecap="round" />
    </svg>
  )
}

const NAV = [
  {
    label: 'Home',
    icon: <path d="M4 11l8-7 8 7M6 10v9h12v-9" stroke="currentColor" strokeWidth="1.7" fill="none" strokeLinecap="round" strokeLinejoin="round" />,
  },
  {
    label: 'My Cities',
    icon: (
      <>
        <rect x="4" y="9" width="6" height="11" stroke="currentColor" strokeWidth="1.7" fill="none" rx="1" />
        <rect x="12" y="4" width="6" height="16" stroke="currentColor" strokeWidth="1.7" fill="none" rx="1" />
      </>
    ),
  },
  {
    label: 'Manage City',
    icon: (
      <>
        <path d="M12 3l7 3v5c0 4.5-3 8-7 10-4-2-7-5.5-7-10V6l7-3z" stroke="currentColor" strokeWidth="1.7" fill="none" strokeLinejoin="round" />
        <path d="M9 12l2 2 4-4.5" stroke="currentColor" strokeWidth="1.7" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      </>
    ),
  },
  {
    label: 'Templates',
    icon: (
      <>
        <rect x="4" y="4" width="16" height="16" rx="3" stroke="currentColor" strokeWidth="1.7" fill="none" />
        <path d="M8.5 11.5l2.5 2.5 4.5-5" stroke="currentColor" strokeWidth="1.7" fill="none" strokeLinecap="round" strokeLinejoin="round" />
      </>
    ),
  },
  {
    label: 'Saved',
    icon: <path d="M6 4h12v16l-6-4-6 4V4z" stroke="currentColor" strokeWidth="1.7" fill="none" strokeLinejoin="round" />,
  },
  {
    label: 'Settings',
    icon: (
      <>
        <circle cx="12" cy="12" r="3" stroke="currentColor" strokeWidth="1.7" fill="none" />
        <path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M19.1 4.9L17 7M7 17l-2.1 2.1" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
      </>
    ),
  },
]

export default function Sidebar({ onExit, onStudio, onMyCities, onManageCity, active: activeProp, collapsed }) {
  const [activeState, setActiveState] = useState('Home')
  const active = activeProp || activeState

  function handleNavClick(label) {
    setActiveState(label)
    if (label === 'My Cities') onMyCities?.()
    else if (label === 'Manage City') onManageCity?.()
    else if (label === 'Home') onStudio?.()
  }

  return (
    <aside className={`side${collapsed ? ' side--collapsed' : ''}`} aria-hidden={collapsed}>
      <button className="side__brand" onClick={onExit} title="Exit to home">
        <img src="/logo-transparent.png" alt="CEWNity Logo" className="side__logo-img" />
      </button>

      <nav className="side__nav">
        {NAV.map((n) => (
          <button
            key={n.label}
            className={`side__item${active === n.label ? ' is-active' : ''}`}
            onClick={() => handleNavClick(n.label)}
          >
            <svg width="22" height="22" viewBox="0 0 24 24">
              {n.icon}
            </svg>
            {n.label}
          </button>
        ))}
      </nav>

      <div className="side__foot">
        <img src="/logo-mark.png" alt="CEWNity Emblem" className="side__foot-mark" />
        <div>
          CEWNity.
          <br />
          A greener tomorrow.
        </div>
      </div>
    </aside>
  )
}
