import { useCallback, useEffect, useRef, useState } from 'react'
import { ConfigPanel } from '@/features/world/ConfigPanel'
import { WORLD_PRESETS } from '@/features/world/WorldPresets'
import { WorldRenderer } from '@/rendering/WorldRenderer'
import { SimulationConnection } from '@/transport/SimulationConnection'
import { generateWorld, type GeneratedWorld, type SimulationSnapshot, type WorldConfig } from '@/transport/WorldApi'
import { SimulationControls } from '@/features/world/SimulationControls'
import './globals.css'
import './App.css'

export function App() {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const rendererRef = useRef<WorldRenderer | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const connectionRef = useRef<SimulationConnection | null>(null)
  const autoGenerationStartedRef = useRef(false)
  const [world, setWorld] = useState<GeneratedWorld | null>(null)
  const [telemetry, setTelemetry] = useState<SimulationSnapshot | null>(null)
  const telemetryWindowRef = useRef<{ startedAt: number, snapshotCount: number, actionTotals: Record<string, number> } | null>(null)
  const [running, setRunning] = useState(false)
  const [speed, setSpeed] = useState('1x')

  const handleGenerate = useCallback(async (config: WorldConfig) => {
    setIsLoading(true)
    setError(null)
    try {
      const data = await generateWorld(config)
      connectionRef.current?.stop()
      setWorld(data)
      setTelemetry(data.snapshot)
      telemetryWindowRef.current = null
      setRunning(data.snapshot.isRunning)
      setSpeed(data.snapshot.speed)
      if (rendererRef.current) {
        rendererRef.current.renderWorld(data.staticWorld)
      }
      const connection = new SimulationConnection(data.sessionToken, snapshot => {
        rendererRef.current?.updateOrganisms(snapshot.organisms)
        setRunning(snapshot.isRunning)
        setSpeed(snapshot.speed)
        setWorld(current => current ? { ...current, snapshot } : current)
        const now = Date.now()
        const window = telemetryWindowRef.current ?? { startedAt: now, snapshotCount: 0, actionTotals: {} }
        window.snapshotCount++
        for (const [action, count] of Object.entries(snapshot.actions)) {
          window.actionTotals[action] = (window.actionTotals[action] ?? 0) + count
        }
        if (now - window.startedAt >= 1_000) {
          const actions = Object.fromEntries(Object.entries(window.actionTotals).map(([action, count]) => [action, Math.round(count / window.snapshotCount)]))
          setTelemetry({ ...snapshot, actions })
          telemetryWindowRef.current = null
        } else {
          telemetryWindowRef.current = window
        }
      })
      connectionRef.current = connection
      await connection.start()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate world. Is backend running on port 5184?')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    const renderer = new WorldRenderer()
    rendererRef.current = renderer

    renderer.init(container)
    if (!autoGenerationStartedRef.current) {
      autoGenerationStartedRef.current = true
      handleGenerate(WORLD_PRESETS[0].config)
    }

    const handleResize = () => {
      renderer.resize()
    }

    window.addEventListener('resize', handleResize)

    return () => {
      window.removeEventListener('resize', handleResize)
      renderer.destroy()
      connectionRef.current?.stop()
      rendererRef.current = null
    }
  }, [handleGenerate])

  const toggleSimulation = async () => {
    await connectionRef.current?.command(!running, speed)
    setRunning(!running)
  }

  const changeSpeed = async (nextSpeed: string) => {
    setSpeed(nextSpeed)
    if (running) await connectionRef.current?.command(true, nextSpeed)
  }

  return (
    <div className="app-container">
      <ConfigPanel
        onGenerate={handleGenerate}
        isLoading={isLoading}
        fingerprint={world?.snapshot.fingerprint}
      />
      <main className="canvas-viewport">
        <div ref={containerRef} className="viewport-canvas-container" />
        <header className="viewport-header">
          <div>
            <span className="viewport-eyebrow">Live environment</span>
            <h2>World observation</h2>
          </div>
          <div className={`status-pill ${error ? 'status-error' : ''}`}>
            <span className="status-dot" />
            {error ? 'Connection issue' : isLoading ? 'Generating' : 'System ready'}
          </div>
        </header>

        {world && !isLoading && (
          <div className="world-metrics" aria-label="World statistics">
            <div className="metric-item">
              <span>Tick</span>
              <strong>{world.snapshot.tick}</strong>
            </div>
            <div className="metric-divider" />
            <div className="metric-item">
              <span>Grid</span>
              <strong>{world.staticWorld.width} × {world.staticWorld.height}</strong>
            </div>
            <div className="metric-divider" />
            <div className="metric-item">
              <span>Total</span>
              <strong>{world.snapshot.population}</strong>
            </div>
            <div className="metric-divider" />
            <div className="metric-item">
              <span style={{ color: '#facc15' }}>Herbivores</span>
              <strong style={{ color: '#fef08a' }}>{world.snapshot.herbivores ?? world.snapshot.organisms.filter(o => o.species === 'Herbivore').length}</strong>
            </div>
            <div className="metric-divider" />
            <div className="metric-item">
              <span style={{ color: '#ef4444' }}>Carnivores</span>
              <strong style={{ color: '#fca5a5' }}>{world.snapshot.carnivores ?? world.snapshot.organisms.filter(o => o.species === 'Carnivore').length}</strong>
            </div>
          </div>
        )}
        {world && telemetry && !isLoading && (
          <section className="survival-telemetry" aria-label="Survival telemetry">
            <span className="telemetry-title">Population</span>
            <div className="telemetry-values">
              <span>Herbivores: <strong style={{ color: '#facc15' }}>{world.snapshot.herbivores ?? world.snapshot.organisms.filter(o => o.species === 'Herbivore').length}</strong></span>
              <span>Carnivores: <strong style={{ color: '#ef4444' }}>{world.snapshot.carnivores ?? world.snapshot.organisms.filter(o => o.species === 'Carnivore').length}</strong></span>
            </div>
            <span className="telemetry-title">Average active actions · last second</span>
            <div className="telemetry-values">
              {['Explore', 'SeekFood', 'Eat', 'SeekWater', 'Drink', 'Rest', 'Hunt', 'Attack', 'Flee'].map(action => <span key={action}>{action}: <strong>{telemetry.actions[action] ?? 0}</strong></span>)}
            </div>
            <span className="telemetry-title">Deaths</span>
            <div className="telemetry-values">
              {['Starvation', 'Dehydration', 'OldAge', 'Predation'].map(cause => <span key={cause}>{cause}: <strong>{telemetry.deaths[cause] ?? 0}</strong></span>)}
            </div>
          </section>
        )}
        {world && <SimulationControls running={running} speed={speed} disabled={isLoading || Boolean(error)} onToggle={toggleSimulation} onSpeed={changeSpeed} />}

        <div className="viewport-hint">
          <span className="mouse-icon" aria-hidden="true" />
          Drag to explore&nbsp;&nbsp;·&nbsp;&nbsp;Scroll to zoom
        </div>

        {isLoading && (
          <div className="loading-overlay">
            <span className="loading-spinner" />
            Building ecosystem
          </div>
        )}
        {error && <div className="error-banner"><span aria-hidden="true">!</span>{error}</div>}
      </main>
    </div>
  )
}
