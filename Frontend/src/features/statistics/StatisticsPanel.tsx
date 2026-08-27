import { useMemo, useState } from 'react'
import type { EcosystemStatisticsSummary, SimulationHistoryPoint } from '@/transport/WorldApi'
import './StatisticsPanel.css'

export interface StatisticsPanelProps {
  statistics?: EcosystemStatisticsSummary
  history?: SimulationHistoryPoint[]
  isOpen: boolean
  onToggle: () => void
}

type TabType = 'overview' | 'population' | 'genetics' | 'mortality'

const CAUSE_COLORS: Record<string, string> = {
  Starvation: '#f97316',
  Dehydration: '#06b6d4',
  OldAge: '#a855f7',
  Predation: '#ef4444',
  Combat: '#ec4899',
}

export function StatisticsPanel({ statistics, history = [], isOpen, onToggle }: StatisticsPanelProps) {
  const [activeTab, setActiveTab] = useState<TabType>('overview')
  const [selectedTrait, setSelectedTrait] = useState<'speed' | 'size' | 'vision'>('speed')
  const [hoverIndex, setHoverIndex] = useState<number | null>(null)

  const hoveredPoint = hoverIndex !== null && history[hoverIndex] ? history[hoverIndex] : null

  return (
    <aside className={`statistics-panel ${isOpen ? 'is-open' : 'is-closed'}`} aria-label="Ecosystem Analytics Panel">
      <button
        type="button"
        className="statistics-toggle-handle"
        onClick={onToggle}
        title={isOpen ? 'Collapse Analytics Panel' : 'Expand Analytics Panel'}
      >
        <span className="handle-icon">{isOpen ? '▶' : '◀'}</span>
        <span className="handle-label">Analytics</span>
      </button>

      {isOpen && (
        <div className="statistics-content">
          <header className="statistics-header">
            <div>
              <span className="statistics-eyebrow">Demographic telemetry</span>
              <h3>Ecosystem Analytics</h3>
            </div>
            <button type="button" className="close-btn" onClick={onToggle} aria-label="Close Analytics">
              ✕
            </button>
          </header>

          <nav className="statistics-tabs" aria-label="Analytics subtabs">
            <button
              type="button"
              className={`tab-btn ${activeTab === 'overview' ? 'active' : ''}`}
              onClick={() => { setActiveTab('overview'); setHoverIndex(null) }}
            >
              Overview
            </button>
            <button
              type="button"
              className={`tab-btn ${activeTab === 'population' ? 'active' : ''}`}
              onClick={() => { setActiveTab('population'); setHoverIndex(null) }}
            >
              Population
            </button>
            <button
              type="button"
              className={`tab-btn ${activeTab === 'genetics' ? 'active' : ''}`}
              onClick={() => { setActiveTab('genetics'); setHoverIndex(null) }}
            >
              Genetics
            </button>
            <button
              type="button"
              className={`tab-btn ${activeTab === 'mortality' ? 'active' : ''}`}
              onClick={() => { setActiveTab('mortality'); setHoverIndex(null) }}
            >
              Mortality
            </button>
          </nav>

          <div className="tab-body">
            {activeTab === 'overview' && (
              <div className="overview-tab">
                <div className="kpi-grid">
                  <div className="kpi-card">
                    <span className="kpi-label">Total Population</span>
                    <strong className="kpi-value text-accent">{statistics?.totalPopulation ?? 0}</strong>
                    <div className="kpi-sub">
                      <span className="text-herbivore">H: {statistics?.herbivores ?? 0}</span>
                      <span className="text-carnivore">C: {statistics?.carnivores ?? 0}</span>
                    </div>
                  </div>

                  <div className="kpi-card">
                    <span className="kpi-label">Natality & Mortality</span>
                    <div className="kpi-dual">
                      <div>
                        <span className="kpi-sublabel">Births</span>
                        <strong className="kpi-val-small text-herbivore">+{statistics?.totalBirths ?? 0}</strong>
                      </div>
                      <div className="kpi-vdiv" />
                      <div>
                        <span className="kpi-sublabel">Deaths</span>
                        <strong className="kpi-val-small text-carnivore">-{statistics?.totalDeaths ?? 0}</strong>
                      </div>
                    </div>
                  </div>

                  <div className="kpi-card">
                    <span className="kpi-label">Avg Lifespan</span>
                    <strong className="kpi-value text-info">
                      {statistics?.mortality.averageLifespanTicks.toFixed(0) ?? '0'} <small>ticks</small>
                    </strong>
                    <div className="kpi-sub">
                      <span>Max: {statistics?.mortality.maxLifespanTicks.toFixed(0) ?? '0'}</span>
                    </div>
                  </div>

                  <div className="kpi-card">
                    <span className="kpi-label">Average Speed</span>
                    <strong className="kpi-value text-vision">
                      {statistics?.overallTraits.averageSpeed.toFixed(2) ?? '0.00'}
                    </strong>
                    <div className="kpi-sub">
                      <span className="text-herbivore">H: {statistics?.herbivoreTraits.averageSpeed.toFixed(2) ?? '0.00'}</span>
                      <span className="text-carnivore">C: {statistics?.carnivoreTraits.averageSpeed.toFixed(2) ?? '0.00'}</span>
                    </div>
                  </div>
                </div>

                <div className="section-card">
                  <h4 className="card-heading">Species Trait Baseline</h4>
                  <div className="traits-comparison-table">
                    <div className="traits-row traits-header">
                      <span>Trait</span>
                      <span className="text-herbivore">Herbivores</span>
                      <span className="text-carnivore">Carnivores</span>
                      <span>Overall</span>
                    </div>
                    <div className="traits-row">
                      <span className="trait-name">Speed</span>
                      <span className="text-herbivore font-mono">{statistics?.herbivoreTraits.averageSpeed.toFixed(2) ?? '0.00'}</span>
                      <span className="text-carnivore font-mono">{statistics?.carnivoreTraits.averageSpeed.toFixed(2) ?? '0.00'}</span>
                      <span className="font-mono">{statistics?.overallTraits.averageSpeed.toFixed(2) ?? '0.00'}</span>
                    </div>
                    <div className="traits-row">
                      <span className="trait-name">Size</span>
                      <span className="text-herbivore font-mono">{statistics?.herbivoreTraits.averageSize.toFixed(2) ?? '0.00'}</span>
                      <span className="text-carnivore font-mono">{statistics?.carnivoreTraits.averageSize.toFixed(2) ?? '0.00'}</span>
                      <span className="font-mono">{statistics?.overallTraits.averageSize.toFixed(2) ?? '0.00'}</span>
                    </div>
                    <div className="traits-row">
                      <span className="trait-name">Vision</span>
                      <span className="text-herbivore font-mono">{statistics?.herbivoreTraits.averageVision.toFixed(2) ?? '0.00'}</span>
                      <span className="text-carnivore font-mono">{statistics?.carnivoreTraits.averageVision.toFixed(2) ?? '0.00'}</span>
                      <span className="font-mono">{statistics?.overallTraits.averageVision.toFixed(2) ?? '0.00'}</span>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {activeTab === 'population' && (
              <div className="chart-tab">
                <div className="chart-header">
                  <h4>Population Dynamics</h4>
                  <span className="chart-meta">{history.length} samples</span>
                </div>

                {history.length > 0 ? (
                  <PopulationSvgChart
                    history={history}
                    hoverIndex={hoverIndex}
                    onHoverIndex={setHoverIndex}
                  />
                ) : (
                  <div className="empty-chart">Sampling population dynamics...</div>
                )}

                {hoveredPoint && (
                  <div className="chart-tooltip-bar">
                    <span className="tooltip-tick font-mono">Tick #{hoveredPoint.tick}</span>
                    <span className="text-info font-mono">Total: {hoveredPoint.totalPopulation}</span>
                    <span className="text-herbivore font-mono">H: {hoveredPoint.herbivoreCount}</span>
                    <span className="text-carnivore font-mono">C: {hoveredPoint.carnivoreCount}</span>
                  </div>
                )}
              </div>
            )}

            {activeTab === 'genetics' && (
              <div className="chart-tab">
                <div className="chart-header">
                  <h4>Evolutionary Trait Drift</h4>
                  <div className="trait-selector">
                    <button
                      type="button"
                      className={`trait-chip ${selectedTrait === 'speed' ? 'active' : ''}`}
                      onClick={() => setSelectedTrait('speed')}
                    >
                      Speed
                    </button>
                    <button
                      type="button"
                      className={`trait-chip ${selectedTrait === 'size' ? 'active' : ''}`}
                      onClick={() => setSelectedTrait('size')}
                    >
                      Size
                    </button>
                    <button
                      type="button"
                      className={`trait-chip ${selectedTrait === 'vision' ? 'active' : ''}`}
                      onClick={() => setSelectedTrait('vision')}
                    >
                      Vision
                    </button>
                  </div>
                </div>

                {history.length > 0 ? (
                  <GeneticsSvgChart
                    history={history}
                    trait={selectedTrait}
                    hoverIndex={hoverIndex}
                    onHoverIndex={setHoverIndex}
                  />
                ) : (
                  <div className="empty-chart">Sampling genome traits...</div>
                )}

                {hoveredPoint && (
                  <div className="chart-tooltip-bar">
                    <span className="tooltip-tick font-mono">Tick #{hoveredPoint.tick}</span>
                    <span className="text-herbivore font-mono">
                      H {selectedTrait}: {getTraitVal(hoveredPoint.herbivoreTraits, selectedTrait).toFixed(2)}
                    </span>
                    <span className="text-carnivore font-mono">
                      C {selectedTrait}: {getTraitVal(hoveredPoint.carnivoreTraits, selectedTrait).toFixed(2)}
                    </span>
                  </div>
                )}
              </div>
            )}

            {activeTab === 'mortality' && (
              <div className="chart-tab">
                <div className="chart-header">
                  <h4>Mortality Breakdown by Cause</h4>
                  <span className="chart-meta">Total: {statistics?.mortality.totalDeaths ?? 0}</span>
                </div>

                <MortalitySvgChart deathsByCause={statistics?.mortality.deathsByCause} />

                <div className="lifespan-summary">
                  <div className="lifespan-item">
                    <span>Herbivore Avg Lifespan</span>
                    <strong className="text-herbivore">{statistics?.mortality.herbivoreAverageLifespanTicks.toFixed(0) ?? '0'} ticks</strong>
                  </div>
                  <div className="lifespan-item">
                    <span>Carnivore Avg Lifespan</span>
                    <strong className="text-carnivore">{statistics?.mortality.carnivoreAverageLifespanTicks.toFixed(0) ?? '0'} ticks</strong>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </aside>
  )
}

function getTraitVal(traits: { averageSpeed: number, averageSize: number, averageVision: number }, trait: 'speed' | 'size' | 'vision') {
  if (trait === 'speed') return traits.averageSpeed
  if (trait === 'size') return traits.averageSize
  return traits.averageVision
}

function PopulationSvgChart({
  history,
  hoverIndex,
  onHoverIndex,
}: {
  history: SimulationHistoryPoint[]
  hoverIndex: number | null
  onHoverIndex: (idx: number | null) => void
}) {
  const width = 340
  const height = 200
  const padding = { top: 15, right: 10, bottom: 25, left: 35 }
  const innerWidth = width - padding.left - padding.right
  const innerHeight = height - padding.top - padding.bottom

  const maxVal = useMemo(() => {
    let max = 10
    for (const p of history) {
      if (p.totalPopulation > max) max = p.totalPopulation
    }
    return Math.ceil(max * 1.15)
  }, [history])

  const points = useMemo(() => {
    const len = history.length
    return history.map((p, i) => {
      const x = padding.left + (len > 1 ? (i / (len - 1)) * innerWidth : innerWidth / 2)
      const yTotal = padding.top + innerHeight - (p.totalPopulation / maxVal) * innerHeight
      const yHerb = padding.top + innerHeight - (p.herbivoreCount / maxVal) * innerHeight
      const yCarn = padding.top + innerHeight - (p.carnivoreCount / maxVal) * innerHeight
      return { x, yTotal, yHerb, yCarn, p }
    })
  }, [history, maxVal, innerWidth, innerHeight, padding.left, padding.top])

  const makeAreaPath = (getY: (p: (typeof points)[0]) => number) => {
    if (points.length === 0) return ''
    const linePart = points.map((pt, i) => `${i === 0 ? 'M' : 'L'} ${pt.x.toFixed(1)} ${getY(pt).toFixed(1)}`).join(' ')
    const bottomY = (padding.top + innerHeight).toFixed(1)
    const closePart = ` L ${points[points.length - 1].x.toFixed(1)} ${bottomY} L ${points[0].x.toFixed(1)} ${bottomY} Z`
    return linePart + closePart
  }

  const makeLinePath = (getY: (p: (typeof points)[0]) => number) => {
    if (points.length === 0) return ''
    return points.map((pt, i) => `${i === 0 ? 'M' : 'L'} ${pt.x.toFixed(1)} ${getY(pt).toFixed(1)}`).join(' ')
  }

  const firstTick = history[0]?.tick ?? 0
  const lastTick = history[history.length - 1]?.tick ?? 0

  return (
    <div className="chart-wrapper">
      <svg
        width="100%"
        height={height}
        viewBox={`0 0 ${width} ${height}`}
        className="native-chart-svg"
        onMouseLeave={() => onHoverIndex(null)}
        onMouseMove={e => {
          const rect = e.currentTarget.getBoundingClientRect()
          const svgX = ((e.clientX - rect.left) / rect.width) * width
          const relativeX = Math.max(0, Math.min(innerWidth, svgX - padding.left))
          const idx = Math.round((relativeX / innerWidth) * (history.length - 1))
          onHoverIndex(Math.max(0, Math.min(history.length - 1, idx)))
        }}
      >
        <defs>
          <linearGradient id="gradTotal" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#38bdf8" stopOpacity="0.35" />
            <stop offset="100%" stopColor="#38bdf8" stopOpacity="0.0" />
          </linearGradient>
          <linearGradient id="gradHerb" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#facc15" stopOpacity="0.3" />
            <stop offset="100%" stopColor="#facc15" stopOpacity="0.0" />
          </linearGradient>
          <linearGradient id="gradCarn" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#ef4444" stopOpacity="0.3" />
            <stop offset="100%" stopColor="#ef4444" stopOpacity="0.0" />
          </linearGradient>
        </defs>

        {/* Grid lines */}
        {[0, 0.25, 0.5, 0.75, 1].map(pct => {
          const y = padding.top + innerHeight * (1 - pct)
          const val = Math.round(maxVal * pct)
          return (
            <g key={pct}>
              <line
                x1={padding.left}
                y1={y}
                x2={padding.left + innerWidth}
                y2={y}
                stroke="rgba(255, 255, 255, 0.08)"
                strokeDasharray="2,2"
              />
              <text
                x={padding.left - 6}
                y={y + 3}
                textAnchor="end"
                fill="#64748b"
                fontSize="9"
                fontFamily="monospace"
              >
                {val}
              </text>
            </g>
          )
        })}

        {/* X Axis labels */}
        <text x={padding.left} y={height - 6} fill="#64748b" fontSize="9" fontFamily="monospace">
          T:{firstTick}
        </text>
        <text x={padding.left + innerWidth} y={height - 6} textAnchor="end" fill="#64748b" fontSize="9" fontFamily="monospace">
          T:{lastTick}
        </text>

        {/* Areas */}
        <path d={makeAreaPath(p => p.yTotal)} fill="url(#gradTotal)" />
        <path d={makeAreaPath(p => p.yHerb)} fill="url(#gradHerb)" />
        <path d={makeAreaPath(p => p.yCarn)} fill="url(#gradCarn)" />

        {/* Lines */}
        <path d={makeLinePath(p => p.yTotal)} fill="none" stroke="#38bdf8" strokeWidth="1.5" />
        <path d={makeLinePath(p => p.yHerb)} fill="none" stroke="#facc15" strokeWidth="1.5" />
        <path d={makeLinePath(p => p.yCarn)} fill="none" stroke="#ef4444" strokeWidth="1.5" />

        {/* Hover crosshair */}
        {hoverIndex !== null && points[hoverIndex] && (
          <g>
            <line
              x1={points[hoverIndex].x}
              y1={padding.top}
              x2={points[hoverIndex].x}
              y2={padding.top + innerHeight}
              stroke="rgba(255, 255, 255, 0.35)"
              strokeWidth="1"
              strokeDasharray="2,2"
            />
            <circle cx={points[hoverIndex].x} cy={points[hoverIndex].yTotal} r="3" fill="#38bdf8" />
            <circle cx={points[hoverIndex].x} cy={points[hoverIndex].yHerb} r="3" fill="#facc15" />
            <circle cx={points[hoverIndex].x} cy={points[hoverIndex].yCarn} r="3" fill="#ef4444" />
          </g>
        )}
      </svg>
      <div className="chart-legend">
        <span className="legend-chip"><i style={{ background: '#38bdf8' }} /> Total</span>
        <span className="legend-chip"><i style={{ background: '#facc15' }} /> Herbivores</span>
        <span className="legend-chip"><i style={{ background: '#ef4444' }} /> Carnivores</span>
      </div>
    </div>
  )
}

function GeneticsSvgChart({
  history,
  trait,
  hoverIndex,
  onHoverIndex,
}: {
  history: SimulationHistoryPoint[]
  trait: 'speed' | 'size' | 'vision'
  hoverIndex: number | null
  onHoverIndex: (idx: number | null) => void
}) {
  const width = 340
  const height = 200
  const padding = { top: 15, right: 10, bottom: 25, left: 35 }
  const innerWidth = width - padding.left - padding.right
  const innerHeight = height - padding.top - padding.bottom

  const maxVal = useMemo(() => {
    let max = 2.0
    for (const p of history) {
      const h = getTraitVal(p.herbivoreTraits, trait)
      const c = getTraitVal(p.carnivoreTraits, trait)
      if (h > max) max = h
      if (c > max) max = c
    }
    return max * 1.2
  }, [history, trait])

  const points = useMemo(() => {
    const len = history.length
    return history.map((p, i) => {
      const x = padding.left + (len > 1 ? (i / (len - 1)) * innerWidth : innerWidth / 2)
      const hVal = getTraitVal(p.herbivoreTraits, trait)
      const cVal = getTraitVal(p.carnivoreTraits, trait)
      const yHerb = padding.top + innerHeight - (hVal / maxVal) * innerHeight
      const yCarn = padding.top + innerHeight - (cVal / maxVal) * innerHeight
      return { x, yHerb, yCarn, p }
    })
  }, [history, trait, maxVal, innerWidth, innerHeight, padding.left, padding.top])

  const makeLinePath = (getY: (p: (typeof points)[0]) => number) => {
    if (points.length === 0) return ''
    return points.map((pt, i) => `${i === 0 ? 'M' : 'L'} ${pt.x.toFixed(1)} ${getY(pt).toFixed(1)}`).join(' ')
  }

  const firstTick = history[0]?.tick ?? 0
  const lastTick = history[history.length - 1]?.tick ?? 0

  return (
    <div className="chart-wrapper">
      <svg
        width="100%"
        height={height}
        viewBox={`0 0 ${width} ${height}`}
        className="native-chart-svg"
        onMouseLeave={() => onHoverIndex(null)}
        onMouseMove={e => {
          const rect = e.currentTarget.getBoundingClientRect()
          const svgX = ((e.clientX - rect.left) / rect.width) * width
          const relativeX = Math.max(0, Math.min(innerWidth, svgX - padding.left))
          const idx = Math.round((relativeX / innerWidth) * (history.length - 1))
          onHoverIndex(Math.max(0, Math.min(history.length - 1, idx)))
        }}
      >
        {/* Grid lines */}
        {[0, 0.33, 0.66, 1].map(pct => {
          const y = padding.top + innerHeight * (1 - pct)
          const val = (maxVal * pct).toFixed(1)
          return (
            <g key={pct}>
              <line
                x1={padding.left}
                y1={y}
                x2={padding.left + innerWidth}
                y2={y}
                stroke="rgba(255, 255, 255, 0.08)"
                strokeDasharray="2,2"
              />
              <text
                x={padding.left - 6}
                y={y + 3}
                textAnchor="end"
                fill="#64748b"
                fontSize="9"
                fontFamily="monospace"
              >
                {val}
              </text>
            </g>
          )
        })}

        {/* X Axis labels */}
        <text x={padding.left} y={height - 6} fill="#64748b" fontSize="9" fontFamily="monospace">
          T:{firstTick}
        </text>
        <text x={padding.left + innerWidth} y={height - 6} textAnchor="end" fill="#64748b" fontSize="9" fontFamily="monospace">
          T:{lastTick}
        </text>

        {/* Lines */}
        <path d={makeLinePath(p => p.yHerb)} fill="none" stroke="#facc15" strokeWidth="2" />
        <path d={makeLinePath(p => p.yCarn)} fill="none" stroke="#ef4444" strokeWidth="2" />

        {/* Hover crosshair */}
        {hoverIndex !== null && points[hoverIndex] && (
          <g>
            <line
              x1={points[hoverIndex].x}
              y1={padding.top}
              x2={points[hoverIndex].x}
              y2={padding.top + innerHeight}
              stroke="rgba(255, 255, 255, 0.35)"
              strokeWidth="1"
              strokeDasharray="2,2"
            />
            <circle cx={points[hoverIndex].x} cy={points[hoverIndex].yHerb} r="3.5" fill="#facc15" />
            <circle cx={points[hoverIndex].x} cy={points[hoverIndex].yCarn} r="3.5" fill="#ef4444" />
          </g>
        )}
      </svg>
      <div className="chart-legend">
        <span className="legend-chip"><i style={{ background: '#facc15' }} /> Herbivore {trait}</span>
        <span className="legend-chip"><i style={{ background: '#ef4444' }} /> Carnivore {trait}</span>
      </div>
    </div>
  )
}

function MortalitySvgChart({ deathsByCause }: { deathsByCause?: Record<string, number> }) {
  const causes = ['Starvation', 'Dehydration', 'OldAge', 'Predation']
  const total = useMemo(() => {
    if (!deathsByCause) return 0
    return Object.values(deathsByCause).reduce((a, b) => a + b, 0)
  }, [deathsByCause])

  return (
    <div className="chart-wrapper mortality-bar-list">
      {causes.map(cause => {
        const count = deathsByCause?.[cause] ?? 0
        const pct = total > 0 ? (count / total) * 100 : 0
        const color = CAUSE_COLORS[cause] ?? '#94a3b8'

        return (
          <div key={cause} className="mortality-bar-row">
            <div className="mortality-bar-meta">
              <span className="mortality-cause">{cause}</span>
              <strong className="mortality-count font-mono">{count} ({pct.toFixed(0)}%)</strong>
            </div>
            <div className="mortality-bar-track">
              <div
                className="mortality-bar-fill"
                style={{
                  width: `${pct}%`,
                  backgroundColor: color,
                }}
              />
            </div>
          </div>
        )
      })}
    </div>
  )
}
