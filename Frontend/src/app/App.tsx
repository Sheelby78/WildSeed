import { useCallback, useEffect, useRef, useState } from 'react'
import { ConfigPanel } from '@/features/world/ConfigPanel'
import { WORLD_PRESETS } from '@/features/world/WorldPresets'
import { WorldRenderer } from '@/rendering/WorldRenderer'
import { generateWorld, type WorldConfig, type WorldSnapshot } from '@/transport/WorldApi'
import './globals.css'
import './App.css'

export function App() {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const rendererRef = useRef<WorldRenderer | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [snapshot, setSnapshot] = useState<WorldSnapshot | null>(null)

  const handleGenerate = useCallback(async (config: WorldConfig) => {
    setIsLoading(true)
    setError(null)
    try {
      const data = await generateWorld(config)
      setSnapshot(data)
      if (rendererRef.current) {
        rendererRef.current.renderWorld(data)
      }
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

    let isMounted = true

    renderer.init(container).then(() => {
      if (isMounted) {
        handleGenerate(WORLD_PRESETS[0].config)
      }
    })

    const handleResize = () => {
      renderer.resize()
    }

    window.addEventListener('resize', handleResize)

    return () => {
      isMounted = false
      window.removeEventListener('resize', handleResize)
      renderer.destroy()
      rendererRef.current = null
    }
  }, [handleGenerate])

  return (
    <div className="app-container">
      <ConfigPanel
        onGenerate={handleGenerate}
        isLoading={isLoading}
        fingerprint={snapshot?.fingerprint}
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

        {snapshot && !isLoading && (
          <div className="world-metrics" aria-label="World statistics">
            <div className="metric-item">
              <span>Grid</span>
              <strong>{snapshot.width} × {snapshot.height}</strong>
            </div>
            <div className="metric-divider" />
            <div className="metric-item">
              <span>Population</span>
              <strong>{snapshot.organisms.length}</strong>
            </div>
          </div>
        )}

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
