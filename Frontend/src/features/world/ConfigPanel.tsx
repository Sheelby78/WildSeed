import { useState } from 'react'
import type { WorldConfig } from '@/transport/WorldApi'
import { Icon } from '@/shared/Icon'
import { WORLD_PRESETS } from './WorldPresets'
import './ConfigPanel.css'

interface ConfigPanelProps {
  onGenerate: (config: WorldConfig) => void
  isLoading: boolean
  fingerprint?: string
  onClose: () => void
}

const PRESET_NAMES: Record<string, string> = { island: 'Archipelago', continental: 'Continental', arid: 'Arid wasteland', lush: 'Lush basin' }

export function ConfigPanel({ onGenerate, isLoading, fingerprint, onClose }: ConfigPanelProps) {
  const [selectedPresetId, setSelectedPresetId] = useState('island')
  const [showAdvanced, setShowAdvanced] = useState(false)
  const [config, setConfig] = useState<WorldConfig>(() => ({ ...WORLD_PRESETS[0].config }))
  const selectedPreset = WORLD_PRESETS.find(preset => preset.id === selectedPresetId)

  const handlePresetChange = (presetId: string) => {
    setSelectedPresetId(presetId)
    const preset = WORLD_PRESETS.find(preset => preset.id === presetId)
    if (preset) setConfig({ ...preset.config })
  }

  return (
    <aside className="sidebar" aria-label="World configuration">
      <div className="sidebar-heading"><Icon name="sliders" size={16} /><h2>World setup</h2><button type="button" className="icon-button setup-close" onClick={onClose} aria-label="Close setup"><Icon name="close" size={14} /></button></div>
      <form id="world-config" onSubmit={event => { event.preventDefault(); onGenerate(config) }} className="config-form">
        <fieldset disabled={isLoading}>
          <legend className="field-label">Choose an environment</legend>
          <div className="preset-grid">
            {WORLD_PRESETS.map(preset => (
              <button type="button" key={preset.id} aria-pressed={selectedPresetId === preset.id} className={`preset-card ${selectedPresetId === preset.id ? 'selected' : ''}`} onClick={() => handlePresetChange(preset.id)}>
                <span className={`preset-art preset-${preset.id}`} aria-hidden="true"><span /><i /></span>
                <span className="preset-name">{PRESET_NAMES[preset.id]}<span className="preset-radio" /></span>
              </button>
            ))}
          </div>
          <p className="preset-description">{selectedPreset?.description}</p>

          <div className="form-group">
            <label className="field-label" htmlFor="world-seed">World seed <span>Reproducible by design</span></label>
            <div className="seed-row">
              <span className="seed-symbol"><Icon name="hash" size={14} /></span>
              <input id="world-seed" type="number" required min="-2147483648" max="2147483647" value={config.seed ?? 1337} onChange={event => setConfig(prev => ({ ...prev, seed: parseInt(event.target.value) || 0 }))} className="text-input" />
              <button type="button" onClick={() => setConfig(prev => ({ ...prev, seed: Math.floor(Math.random() * 1000000) }))} className="icon-button" aria-label="Randomize seed" title="Randomize seed"><Icon name="shuffle" size={15} /></button>
            </div>
          </div>

          <div className="form-group">
            <label className="field-label" htmlFor="map-dimensions">Map dimensions</label>
            <select id="map-dimensions" value={`${config.width ?? 128}x${config.height ?? 128}`} onChange={event => {
              const [width, height] = event.target.value.split('x').map(Number)
              setConfig(prev => ({ ...prev, width, height }))
            }} className="select-input">
              <option value="128x128">128 × 128 · 16K tiles</option>
              <option value="192x192">192 × 192 · 36K tiles</option>
              <option value="256x256">256 × 256 · 65K tiles</option>
            </select>
          </div>

          <div className="config-section-label"><span className="eyebrow">Initial population</span><Icon name="activity" size={14} /></div>
          <div className="grid-2col">
            <div className="form-group">
              <label className="field-label" htmlFor="herbivores"><i className="species-dot herbivore" />Herbivores</label>
              <input id="herbivores" type="number" min="0" max="1000" required value={config.initialHerbivores ?? 50} onChange={event => setConfig(prev => ({ ...prev, initialHerbivores: Math.max(0, parseInt(event.target.value) || 0) }))} className="text-input" />
            </div>
            <div className="form-group">
              <label className="field-label" htmlFor="carnivores"><i className="species-dot carnivore" />Carnivores</label>
              <input id="carnivores" type="number" min="0" max="500" required value={config.initialCarnivores ?? 10} onChange={event => setConfig(prev => ({ ...prev, initialCarnivores: Math.max(0, parseInt(event.target.value) || 0) }))} className="text-input" />
            </div>
          </div>

          <button type="button" className="advanced-toggle-btn" aria-expanded={showAdvanced} aria-controls="advanced-fields" onClick={() => setShowAdvanced(prev => !prev)}>
            <Icon name="sliders" size={14} /><span>Environment parameters</span><Icon name="chevron" size={14} style={{ transform: showAdvanced ? 'rotate(90deg)' : undefined }} />
          </button>
          {showAdvanced && <div id="advanced-fields" className="advanced-fields">
            <div className="form-group">
              <div className="slider-label-row"><label className="field-label" htmlFor="water-level">Water level</label><output htmlFor="water-level">{Math.round((config.waterLevel ?? .45) * 100)}%</output></div>
              <input id="water-level" type="range" min=".1" max=".85" step=".01" value={config.waterLevel ?? .45} onChange={event => setConfig(prev => ({ ...prev, waterLevel: parseFloat(event.target.value) }))} />
            </div>
            <div className="form-group">
              <div className="slider-label-row"><label className="field-label" htmlFor="vegetation-density">Vegetation density</label><output htmlFor="vegetation-density">{Math.round((config.vegetationDensity ?? .5) * 100)}%</output></div>
              <input id="vegetation-density" type="range" min="0" max="1" step=".01" value={config.vegetationDensity ?? .5} onChange={event => setConfig(prev => ({ ...prev, vegetationDensity: parseFloat(event.target.value) }))} />
            </div>
          </div>}
        </fieldset>
      </form>
      <div className="config-actions">
        <button type="submit" form="world-config" className="generate-button" disabled={isLoading}><Icon name="globe" size={16} /><span>{isLoading ? 'Generating world…' : 'Generate world'}</span><Icon name="arrow" size={15} /></button>
        <p className="generation-hint">Creates a new world. Replaces the current run.</p>
      </div>
      <div className="sidebar-bottom">
        <div className="seed-note"><span className="seed-note-icon"><Icon name="seed" size={21} /></span><div><strong>Small rules. Endless possibilities.</strong><p>Every ecosystem starts with a seed.</p></div></div>
        {fingerprint && <details className="fingerprint-card"><summary>State fingerprint <Icon name="hash" size={13} /></summary><code>{fingerprint}</code><p>Canonical identifier of the current state.</p></details>}
        <div className="sidebar-footer"><span>DETERMINISTIC ENGINE</span><span>v0.1 / alpha</span></div>
      </div>
    </aside>
  )
}
