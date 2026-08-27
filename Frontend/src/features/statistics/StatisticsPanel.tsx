import { useMemo, useState } from 'react'
import type { EcosystemStatisticsSummary, SimulationHistoryPoint, SimulationSnapshot } from '@/transport/WorldApi'
import { Icon } from '@/shared/Icon'
import './StatisticsPanel.css'

export interface StatisticsPanelProps {
  statistics?: EcosystemStatisticsSummary
  history?: SimulationHistoryPoint[]
  telemetry?: SimulationSnapshot
  isOpen: boolean
  onToggle: () => void
}

type TabType = 'overview' | 'population' | 'genetics' | 'mortality' | 'activity'

const CAUSE_COLORS: Record<string, string> = {
  Starvation: '#ffb349',
  Dehydration: '#58d8ed',
  OldAge: '#f6b84d',
  Predation: '#ff7485',
  Combat: '#ff9b62',
}

export function StatisticsPanel({ statistics, history = [], telemetry, isOpen, onToggle }: StatisticsPanelProps) {
  const [activeTab, setActiveTab] = useState<TabType>('overview')
  const [selectedTrait, setSelectedTrait] = useState<'speed' | 'size' | 'vision'>('speed')
  const [hoverIndex, setHoverIndex] = useState<number | null>(null)

  const hoveredPoint = hoverIndex !== null && history[hoverIndex] ? history[hoverIndex] : null

  if (!isOpen) return null

  return (
    <aside id="ecosystem-analytics" className="statistics-panel" aria-label="Ecosystem analytics">
      {isOpen && (
        <div className="statistics-content">
          <header className="statistics-header">
            <div>
              <span className="statistics-eyebrow">THE BIGGER PICTURE</span>
              <h3>Ecosystem insights</h3>
            </div>
            <button type="button" className="icon-button" onClick={onToggle} aria-label="Close analytics">
              <Icon name="close" size={14} />
            </button>
          </header>

          <nav className="statistics-tabs" aria-label="Analytics subtabs">
            <button
              type="button"
              className={`tab-btn ${activeTab === 'overview' ? 'active' : ''}`}
              aria-pressed={activeTab === 'overview'}
              onClick={() => { setActiveTab('overview'); setHoverIndex(null) }}
            >
              Overview
            </button>
            <button
              type="button"
              className={`tab-btn ${activeTab === 'population' ? 'active' : ''}`}
              aria-pressed={activeTab === 'population'}
              onClick={() => { setActiveTab('population'); setHoverIndex(null) }}
            >
              Population
            </button>
            <button
              type="button"
              className={`tab-btn ${activeTab === 'genetics' ? 'active' : ''}`}
              aria-pressed={activeTab === 'genetics'}
              onClick={() => { setActiveTab('genetics'); setHoverIndex(null) }}
            >
              Genetics
            </button>
            <button
              type="button"
              className={`tab-btn ${activeTab === 'mortality' ? 'active' : ''}`}
              aria-pressed={activeTab === 'mortality'}
              onClick={() => { setActiveTab('mortality'); setHoverIndex(null) }}
            >
              Mortality
            </button>
            <button type="button" className={`tab-btn ${activeTab === 'activity' ? 'active' : ''}`} aria-pressed={activeTab === 'activity'} onClick={() => { setActiveTab('activity'); setHoverIndex(null) }}>Activity</button>
          </nav>

          <div className="tab-body">
            {activeTab === 'overview' && (
              <div className="overview-tab">
                <div className="overview-population">
                  <div className="chart-header"><h4>Population balance</h4><span className="chart-meta">{history.length} samples</span></div>
                  {history.length > 0 ? <PopulationSvgChart history={history} hoverIndex={hoverIndex} onHoverIndex={setHoverIndex} /> : <div className="empty-chart">Start the simulation to see population trends.</div>}
                </div>
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
                    <span className="kpi-label">Life cycle</span>
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
                  <h4 className="card-heading">Species traits</h4>
                  <div className="traits-comparison-table">
                    <div className="traits-row traits-header">
                      <span>Trait</span>
                      <span className="text-herbivore">Herb.</span>
                      <span className="text-carnivore">Carn.</span>
                      <span>All</span>
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
                      aria-pressed={selectedTrait === 'speed'}
                      onClick={() => setSelectedTrait('speed')}
                    >
                      Speed
                    </button>
                    <button
                      type="button"
                      className={`trait-chip ${selectedTrait === 'size' ? 'active' : ''}`}
                      aria-pressed={selectedTrait === 'size'}
                      onClick={() => setSelectedTrait('size')}
                    >
                      Size
                    </button>
                    <button
                      type="button"
                      className={`trait-chip ${selectedTrait === 'vision' ? 'active' : ''}`}
                      aria-pressed={selectedTrait === 'vision'}
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
            {activeTab === 'activity' && <div className="activity-tab">
              <div className="chart-header"><h4>Active behaviors</h4><span className="chart-meta">1s average</span></div>
              <p className="insight-description">What the living population is doing right now.</p>
              <div className="activity-list">{['Explore', 'SeekFood', 'Eat', 'SeekWater', 'Drink', 'Rest', 'Hunt', 'Attack', 'Flee', 'Mate'].map(action => {
                const count = telemetry?.actions[action] ?? 0
                const total = Math.max(1, Object.values(telemetry?.actions ?? {}).reduce((sum, value) => sum + value, 0))
                return <div className="activity-row" key={action}><span>{action.replace(/([a-z])([A-Z])/g, '$1 $2')}</span><div className="activity-track"><i style={{ width: `${count / total * 100}%` }} /></div><strong>{count}</strong></div>
              })}</div>
              <div className="section-card"><h4 className="card-heading">Deaths by cause</h4>{['Starvation', 'Dehydration', 'OldAge', 'Predation'].map(cause => <div className="death-row" key={cause}><span>{cause === 'OldAge' ? 'Old age' : cause}</span><strong>{telemetry?.deaths[cause] ?? 0}</strong></div>)}</div>
            </div>}
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
        className="native-chart-svg" role="img" aria-label="Historical simulation chart" preserveAspectRatio="none"
        onMouseLeave={() => onHoverIndex(null)}
        onMouseMove={e => {
          const rect = e.currentTarget.getBoundingClientRect()
          const svgX = ((e.clientX - rect.left) / rect.width) * width
          const relativeX = Math.max(0, Math.min(innerWidth, svgX - padding.left))
          const idx = Math.round((relativeX / innerWidth) * (history.length - 1))
          onHoverIndex(Math.max(0, Math.min(history.length - 1, idx)))
        }}
      >
        <title>Population over simulation ticks</title>
        <defs>
          <linearGradient id="gradTotal" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#38c9ff" stopOpacity="0.35" />
            <stop offset="100%" stopColor="#38c9ff" stopOpacity="0.0" />
          </linearGradient>
          <linearGradient id="gradHerb" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#ffe077" stopOpacity="0.3" />
            <stop offset="100%" stopColor="#ffe077" stopOpacity="0.0" />
          </linearGradient>
          <linearGradient id="gradCarn" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#ff7485" stopOpacity="0.3" />
            <stop offset="100%" stopColor="#ff7485" stopOpacity="0.0" />
          </linearGradient>
        </defs>
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
                fill="#8fa8bc"
                fontSize="9"
                fontFamily="Space Grotesk, monospace"
              >
                {val}
              </text>
            </g>
          )
        })}
        <text x={padding.left} y={height - 6} fill="#8fa8bc" fontSize="9" fontFamily="Space Grotesk, monospace">
          T:{firstTick}
        </text>
        <text x={padding.left + innerWidth} y={height - 6} textAnchor="end" fill="#8fa8bc" fontSize="9" fontFamily="Space Grotesk, monospace">
          T:{lastTick}
        </text>
        <path d={makeAreaPath(p => p.yTotal)} fill="url(#gradTotal)" />
        <path d={makeAreaPath(p => p.yHerb)} fill="url(#gradHerb)" />
        <path d={makeAreaPath(p => p.yCarn)} fill="url(#gradCarn)" />
        <path d={makeLinePath(p => p.yTotal)} fill="none" stroke="#38c9ff" strokeWidth="1.5" />
        <path d={makeLinePath(p => p.yHerb)} fill="none" stroke="#ffe077" strokeWidth="1.5" />
        <path d={makeLinePath(p => p.yCarn)} fill="none" stroke="#ff7485" strokeWidth="1.5" />
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
            <circle cx={points[hoverIndex].x} cy={points[hoverIndex].yTotal} r="3" fill="#38c9ff" />
            <circle cx={points[hoverIndex].x} cy={points[hoverIndex].yHerb} r="3" fill="#ffe077" />
            <circle cx={points[hoverIndex].x} cy={points[hoverIndex].yCarn} r="3" fill="#ff7485" />
          </g>
        )}
      </svg>
      <div className="chart-legend">
        <span className="legend-chip"><i style={{ background: '#38c9ff' }} /> Total</span>
        <span className="legend-chip"><i style={{ background: '#ffe077' }} /> Herbivores</span>
        <span className="legend-chip"><i style={{ background: '#ff7485' }} /> Carnivores</span>
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
        className="native-chart-svg" role="img" aria-label="Historical simulation chart" preserveAspectRatio="none"
        onMouseLeave={() => onHoverIndex(null)}
        onMouseMove={e => {
          const rect = e.currentTarget.getBoundingClientRect()
          const svgX = ((e.clientX - rect.left) / rect.width) * width
          const relativeX = Math.max(0, Math.min(innerWidth, svgX - padding.left))
          const idx = Math.round((relativeX / innerWidth) * (history.length - 1))
          onHoverIndex(Math.max(0, Math.min(history.length - 1, idx)))
        }}
      >
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
                fill="#8fa8bc"
                fontSize="9"
                fontFamily="Space Grotesk, monospace"
              >
                {val}
              </text>
            </g>
          )
        })}
        <text x={padding.left} y={height - 6} fill="#8fa8bc" fontSize="9" fontFamily="Space Grotesk, monospace">
          T:{firstTick}
        </text>
        <text x={padding.left + innerWidth} y={height - 6} textAnchor="end" fill="#8fa8bc" fontSize="9" fontFamily="Space Grotesk, monospace">
          T:{lastTick}
        </text>
        <path d={makeLinePath(p => p.yHerb)} fill="none" stroke="#ffe077" strokeWidth="2" />
        <path d={makeLinePath(p => p.yCarn)} fill="none" stroke="#ff7485" strokeWidth="2" />
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
            <circle cx={points[hoverIndex].x} cy={points[hoverIndex].yHerb} r="3.5" fill="#ffe077" />
            <circle cx={points[hoverIndex].x} cy={points[hoverIndex].yCarn} r="3.5" fill="#ff7485" />
          </g>
        )}
      </svg>
      <div className="chart-legend">
        <span className="legend-chip"><i style={{ background: '#ffe077' }} /> Herbivore {trait}</span>
        <span className="legend-chip"><i style={{ background: '#ff7485' }} /> Carnivore {trait}</span>
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
