import { useCallback, useEffect, useRef, useState } from 'react'
import { ConfigPanel } from '@/features/world/ConfigPanel'
import { WORLD_PRESETS } from '@/features/world/WorldPresets'
import { WorldRenderer } from '@/rendering/WorldRenderer'
import { SimulationConnection } from '@/transport/SimulationConnection'
import { generateWorld, type GeneratedWorld, type SimulationSnapshot, type WorldConfig } from '@/transport/WorldApi'
import { SimulationControls } from '@/features/world/SimulationControls'
import { StatisticsPanel } from '@/features/statistics/StatisticsPanel'
import { Icon } from '@/shared/Icon'
import './globals.css'
import './App.css'

const WIDE_WORKSPACE_QUERY = '(min-width: 1280px) and (min-aspect-ratio: 16/9)'

export function App() {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const setupRef = useRef<HTMLDivElement | null>(null)
  const setupToggleRef = useRef<HTMLButtonElement | null>(null)
  const analyticsToggleRef = useRef<HTMLButtonElement | null>(null)
  const rendererRef = useRef<WorldRenderer | null>(null)
  const connectionRef = useRef<SimulationConnection | null>(null)
  const autoGenerationStartedRef = useRef(false)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [commandError, setCommandError] = useState<string | null>(null)
  const [commandPending, setCommandPending] = useState(false)
  const [world, setWorld] = useState<GeneratedWorld | null>(null)
  const [activeConfig, setActiveConfig] = useState<WorldConfig>(WORLD_PRESETS[0].config)
  const [telemetry, setTelemetry] = useState<SimulationSnapshot | null>(null)
  const telemetryWindowRef = useRef<{ startedAt: number, snapshotCount: number, actionTotals: Record<string, number> } | null>(null)
  const [running, setRunning] = useState(false)
  const [speed, setSpeed] = useState('1x')
  const [fps, setFps] = useState(0)
  const [isAnalyticsOpen, setIsAnalyticsOpen] = useState(() => window.innerWidth >= 900)
  const [isSetupOpen, setIsSetupOpen] = useState(() => window.matchMedia(WIDE_WORKSPACE_QUERY).matches)
  const [isWideLayout, setIsWideLayout] = useState(() => window.matchMedia(WIDE_WORKSPACE_QUERY).matches)
  const [isMapFocused, setIsMapFocused] = useState(false)

  useEffect(() => {
    const query = window.matchMedia(WIDE_WORKSPACE_QUERY)
    const updateLayout = (event: MediaQueryListEvent) => setIsWideLayout(event.matches)
    query.addEventListener('change', updateLayout)
    return () => query.removeEventListener('change', updateLayout)
  }, [])

  useEffect(() => {
    if (!isMapFocused) return
    const exitOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setIsMapFocused(false)
    }
    window.addEventListener('keydown', exitOnEscape)
    return () => window.removeEventListener('keydown', exitOnEscape)
  }, [isMapFocused])

  useEffect(() => {
    if (isSetupOpen && window.matchMedia('(max-width: 899px)').matches) {
      setupRef.current?.scrollIntoView({ block: 'start' })
      setupRef.current?.focus({ preventScroll: true })
    }
  }, [isSetupOpen])

  useEffect(() => {
    if (isAnalyticsOpen && window.matchMedia('(max-width: 899px)').matches) {
      document.getElementById('ecosystem-analytics')?.scrollIntoView({ block: 'start' })
    }
  }, [isAnalyticsOpen])

  const handleGenerate = useCallback(async (config: WorldConfig) => {
    setIsLoading(true)
    setError(null)
    setCommandError(null)
    try {
      const data = await generateWorld(config)
      await connectionRef.current?.stop()
      setWorld(data)
      setActiveConfig(config)
      setTelemetry(data.snapshot)
      telemetryWindowRef.current = null
      setRunning(data.snapshot.isRunning)
      setSpeed(data.snapshot.speed)
      rendererRef.current?.renderWorld(data.staticWorld)
      const connection = new SimulationConnection(data.sessionToken, snapshot => {
        rendererRef.current?.updateOrganisms(snapshot.organisms)
        setRunning(snapshot.isRunning)
        if (snapshot.isRunning) setSpeed(snapshot.speed)
        setWorld(current => current ? { ...current, snapshot } : current)
        const now = Date.now()
        const sampleWindow = telemetryWindowRef.current ?? { startedAt: now, snapshotCount: 0, actionTotals: {} }
        sampleWindow.snapshotCount++
        for (const [action, count] of Object.entries(snapshot.actions)) {
          sampleWindow.actionTotals[action] = (sampleWindow.actionTotals[action] ?? 0) + count
        }
        if (now - sampleWindow.startedAt >= 1_000) {
          const actions = Object.fromEntries(Object.entries(sampleWindow.actionTotals).map(([action, count]) => [action, Math.round(count / sampleWindow.snapshotCount)]))
          setTelemetry({ ...snapshot, actions })
          telemetryWindowRef.current = null
        } else {
          telemetryWindowRef.current = sampleWindow
        }
      })
      connectionRef.current = connection
      await connection.start()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to connect. Check that the simulation server is running.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    const container = containerRef.current
    if (!container) return
    let disposed = false
    const renderer = new WorldRenderer()
    rendererRef.current = renderer
    void renderer.init(container).then(() => {
      if (disposed) return
      if (!autoGenerationStartedRef.current) {
        autoGenerationStartedRef.current = true
        void handleGenerate(WORLD_PRESETS[0].config)
      }
    }).catch(() => {
      if (disposed) return
      setIsLoading(false)
      setError('The world renderer could not start. Check WebGL support and reload the page.')
    })
    const resizeObserver = new ResizeObserver(() => renderer.resize())
    resizeObserver.observe(container)
    const fpsInterval = setInterval(() => setFps(renderer.getFPS()), 1000)
    return () => {
      disposed = true
      clearInterval(fpsInterval)
      resizeObserver.disconnect()
      renderer.destroy()
      void connectionRef.current?.stop()
      rendererRef.current = null
    }
  }, [handleGenerate])

  const toggleSimulation = async () => {
    if (!connectionRef.current || commandPending) return
    setCommandPending(true)
    setCommandError(null)
    try {
      await connectionRef.current.command(!running, speed)
      setRunning(!running)
    } catch {
      setCommandError('The playback command did not reach the server. Please try again.')
    } finally {
      setCommandPending(false)
    }
  }

  const changeSpeed = async (nextSpeed: string) => {
    if (commandPending) return
    setCommandError(null)
    if (!running) {
      setSpeed(nextSpeed)
      return
    }
    setCommandPending(true)
    try {
      await connectionRef.current?.command(true, nextSpeed)
      setSpeed(nextSpeed)
    } catch {
      setCommandError('Unable to change simulation speed. Please try again.')
    } finally {
      setCommandPending(false)
    }
  }

  const snapshot = world?.snapshot
  const generation = snapshot?.organisms.length
    ? (snapshot.organisms.reduce((total, organism) => total + (organism.generation ?? 1), 0) / snapshot.organisms.length).toFixed(1)
    : '—'
  const status = error ? 'Connection issue' : isLoading ? 'Preparing world' : running ? 'Simulation live' : 'Simulation paused'

  return (
    <div className={`app-container ${isMapFocused ? 'map-focused' : ''} ${isWideLayout && isSetupOpen && isAnalyticsOpen && !isMapFocused ? 'wide-workspace' : ''}`}>
      <header className="app-header">
        <a href="/" className="brand" aria-label="Wild Seed home">
          <span className="brand-mark"><Icon name="seed" size={23} /></span>
          <span className="brand-name">wildseed<span className="brand-period">.</span></span>
          <span className="release-badge">LAB</span>
        </a>
        <section className="command-bar" aria-label="Workspace toolbar">
          <SimulationControls running={running} speed={speed} disabled={!world || isLoading || commandPending || Boolean(error)} onToggle={toggleSimulation} onSpeed={changeSpeed} />
          <div className="map-navigation" role="group" aria-label="Map view controls">
            <button className="icon-button" title="Zoom out" aria-label="Zoom out" disabled={!world} onClick={() => rendererRef.current?.zoomBy(.8)}><Icon name="minus" size={14} /></button>
            <button className="icon-button" title="Fit entire world" aria-label="Fit world" disabled={!world} onClick={() => rendererRef.current?.fitWorld()}><Icon name="fit" size={14} /></button>
            <button className="icon-button" title="Fill view" aria-label="Fill view" disabled={!world} onClick={() => rendererRef.current?.fillWorld()}><Icon name="globe" size={14} /></button>
            <button className="icon-button" title="Zoom in" aria-label="Zoom in" disabled={!world} onClick={() => rendererRef.current?.zoomBy(1.25)}><Icon name="plus" size={14} /></button>
          </div>
          <div className="workspace-tools">
            <button className={`toolbar-button ${isMapFocused ? 'active' : ''}`} aria-pressed={isMapFocused} onClick={() => setIsMapFocused(focused => !focused)} title={isMapFocused ? 'Exit focus (Esc)' : 'Give the map the entire workspace'}>
              <Icon name="fit" size={15} /><span>{isMapFocused ? 'Exit focus' : 'Focus map'}</span>
            </button>
            <button ref={setupToggleRef} className={`toolbar-button panel-toggle ${isSetupOpen ? 'active' : ''}`} aria-expanded={isSetupOpen && !isMapFocused} aria-controls="world-setup" onClick={() => setIsSetupOpen(open => !open)}>
              <Icon name="sliders" size={15} /><span>Setup</span>
            </button>
            <button ref={analyticsToggleRef} className={`toolbar-button panel-toggle ${isAnalyticsOpen && (!isSetupOpen || isWideLayout) ? 'active' : ''}`} aria-expanded={isAnalyticsOpen && (!isSetupOpen || isWideLayout) && !isMapFocused} aria-controls="ecosystem-analytics" onClick={() => {
              setIsAnalyticsOpen(open => (isSetupOpen && !isWideLayout) || !open)
              if (!isWideLayout) setIsSetupOpen(false)
            }}>
              <Icon name="chart" size={15} /><span>Analytics</span>
            </button>
          </div>
        </section>
      </header>

      <main className="main-workspace">
        {(error || commandError) && <div className="error-banner" role="alert">
          <span>{error || commandError}</span>
          {error && <button onClick={() => handleGenerate(activeConfig)} disabled={isLoading}>Try again</button>}
        </div>}

        <div className="map-area">
          <section className="world-stage" aria-label="World observation">
            <div className="canvas-viewport">
              <div ref={containerRef} className="viewport-canvas-container" />
              {isLoading && <div className="loading-overlay" role="status">
                <span className="loading-spinner" /><strong>Building your ecosystem</strong><span>Preparing the conditions for life.</span>
              </div>}
              {!world && !isLoading && <div className="empty-world">
                <Icon name="globe" size={38} /><strong>Your world awaits</strong><span>Generate an environment to begin exploring.</span>
              </div>}
            </div>
          </section>
        </div>

        <div className={`inspector ${isSetupOpen ? 'configuring' : ''}`} hidden={isMapFocused}>
          <section className="world-summary" aria-label="World summary">
            <div className="observation-heading">
              <div><span className="eyebrow">ECOSYSTEM LABORATORY</span><h1>World observation<span>.</span></h1></div>
              <div className={`status-pill ${error ? 'status-error' : running ? 'status-live' : ''}`} role="status"><span className="status-dot" />{status}</div>
            </div>
            <div className="session-readout">
              <span className="tick-value">TICK {snapshot?.tick.toLocaleString() ?? '—'}</span>
              <span>{world ? `${world.staticWorld.width} × ${world.staticWorld.height}` : '—'}</span>
              <span>{fps} FPS</span>
            </div>
            <section className="world-metrics" aria-label="World statistics">
              <div className="metric-item"><span><Icon name="activity" size={13} />Population</span><strong>{snapshot?.population.toLocaleString() ?? '—'}</strong></div>
              <div className="metric-item"><span><Icon name="layers" size={13} />Avg. generation</span><strong>{generation}</strong></div>
              <div className="metric-item"><span><i className="species-dot herbivore" />Herbivores</span><strong className="text-herbivore">{snapshot?.herbivores.toLocaleString() ?? '—'}</strong></div>
              <div className="metric-item"><span><i className="species-dot carnivore" />Carnivores</span><strong className="text-carnivore">{snapshot?.carnivores.toLocaleString() ?? '—'}</strong></div>
            </section>
          </section>

          <div id="world-setup" ref={setupRef} className="setup-region" hidden={!isSetupOpen} tabIndex={-1}>
            <ConfigPanel onGenerate={handleGenerate} isLoading={isLoading} fingerprint={snapshot?.fingerprint} onClose={() => {
              setIsSetupOpen(false)
              setupToggleRef.current?.focus()
            }} />
          </div>
          <StatisticsPanel statistics={snapshot?.statistics} history={snapshot?.history} telemetry={telemetry ?? undefined} isOpen={isAnalyticsOpen && (!isSetupOpen || isWideLayout) && !isMapFocused} onToggle={() => {
            setIsAnalyticsOpen(false)
            analyticsToggleRef.current?.focus()
          }} />

          <section className="map-guide" aria-label="Map guide">
            <div className="map-guide-heading"><span className="eyebrow">TERRAIN</span><span className="stage-seed"><Icon name="hash" size={12} />{activeConfig.seed}</span></div>
            <div className="map-legend" aria-label="Terrain legend">
              <span><i className="swatch deep-water" />Deep water</span>
              <span><i className="swatch shallow-water" />Shallows</span>
              <span><i className="swatch sand" />Shore</span>
              <span><i className="swatch grass" />Grassland</span>
              <span><i className="swatch forest" />Forest</span>
            </div>
            <p>Drag to explore · Scroll to zoom<br />Fit world restores the full map.</p>
          </section>
        </div>
      </main>
    </div>
  )
}
