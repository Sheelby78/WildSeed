import { useState } from 'react'
import type { WorldConfig } from '@/transport/WorldApi'
import { WORLD_PRESETS } from './WorldPresets'
import './ConfigPanel.css'

interface ConfigPanelProps {
  onGenerate: (config: WorldConfig) => void
  isLoading: boolean
  fingerprint?: string
}

export function ConfigPanel({ onGenerate, isLoading, fingerprint }: ConfigPanelProps) {
  const [selectedPresetId, setSelectedPresetId] = useState('island')
  const [showAdvanced, setShowAdvanced] = useState(false)
  const [config, setConfig] = useState<WorldConfig>(() => ({
    ...WORLD_PRESETS[0].config,
  }))

  const handlePresetChange = (presetId: string) => {
    setSelectedPresetId(presetId)
    const preset = WORLD_PRESETS.find((p) => p.id === presetId)
    if (preset) {
      setConfig({ ...preset.config })
    }
  }

  const handleRandomizeSeed = () => {
    const newSeed = Math.floor(Math.random() * 1000000)
    setConfig((prev) => ({ ...prev, seed: newSeed }))
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    onGenerate(config)
  }

  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <div className="brand-mark" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none">
            <path d="M12 20V10M12 13c-4 0-7-2-7-6 4 0 7 2 7 6ZM12 10c0-4 3-6 7-6 0 4-3 6-7 6Z" />
          </svg>
        </div>
        <div className="brand-copy">
          <h1 className="logo-title">Wild Seed</h1>
          <span className="version-tag">Ecosystem laboratory</span>
        </div>
        <span className="release-badge">Alpha</span>
      </div>

      <form onSubmit={handleSubmit} className="config-form">
        <div className="section-heading">
          <span>01</span>
          <div>
            <h2>World parameters</h2>
            <p>Define the initial environment</p>
          </div>
        </div>

        <div className="form-group">
          <label className="field-label" htmlFor="biome-preset">Biome preset</label>
          <select
            id="biome-preset"
            value={selectedPresetId}
            onChange={(e) => handlePresetChange(e.target.value)}
            className="select-input"
            disabled={isLoading}
          >
            {WORLD_PRESETS.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
          <p className="preset-description">
            {WORLD_PRESETS.find((p) => p.id === selectedPresetId)?.description}
          </p>
        </div>

        <div className="advanced-toggle-wrapper">
          <button
            type="button"
            className="advanced-toggle-btn"
            onClick={() => setShowAdvanced((prev) => !prev)}
          >
            <span>{showAdvanced ? 'Hide fine controls' : 'Fine-tune parameters'}</span>
            <svg className={showAdvanced ? 'toggle-chevron is-open' : 'toggle-chevron'} viewBox="0 0 16 16" aria-hidden="true">
              <path d="m4 6 4 4 4-4" />
            </svg>
          </button>
        </div>

        {showAdvanced && (
          <div className="advanced-fields">
            <div className="form-group">
              <label className="field-label" htmlFor="world-seed">World seed</label>
              <div className="seed-row">
                <input
                  id="world-seed"
                  type="number"
                  value={config.seed ?? 1337}
                  onChange={(e) => setConfig((prev) => ({ ...prev, seed: parseInt(e.target.value) || 0 }))}
                  className="text-input"
                  disabled={isLoading}
                />
                <button
                  type="button"
                  onClick={handleRandomizeSeed}
                  className="btn-secondary"
                  title="Randomize Seed"
                  disabled={isLoading}
                >
                  <svg viewBox="0 0 20 20" aria-hidden="true">
                    <path d="M5.5 3h9A2.5 2.5 0 0 1 17 5.5v9a2.5 2.5 0 0 1-2.5 2.5h-9A2.5 2.5 0 0 1 3 14.5v-9A2.5 2.5 0 0 1 5.5 3Z" />
                    <path d="M7 7h.01M13 7h.01M10 10h.01M7 13h.01M13 13h.01" />
                  </svg>
                </button>
              </div>
            </div>

            <div className="form-group">
              <label className="field-label">Map dimensions</label>
              <select
                value={`${config.width ?? 128}x${config.height ?? 128}`}
                onChange={(e) => {
                  const [w, h] = e.target.value.split('x').map(Number)
                  setConfig((prev) => ({ ...prev, width: w, height: h }))
                }}
                className="select-input"
                disabled={isLoading}
              >
                <option value="128x128">128 × 128 (16K tiles)</option>
                <option value="192x192">192 × 192 (36K tiles)</option>
                <option value="256x256">256 × 256 (65K tiles)</option>
              </select>
            </div>

            <div className="form-group">
              <div className="slider-label-row">
                <label className="field-label">Water Level</label>
                <span className="slider-val">{Math.round((config.waterLevel ?? 0.45) * 100)}%</span>
              </div>
              <input
                type="range"
                min="0.1"
                max="0.85"
                step="0.01"
                value={config.waterLevel ?? 0.45}
                onChange={(e) => setConfig((prev) => ({ ...prev, waterLevel: parseFloat(e.target.value) }))}
                className="slider-input"
                disabled={isLoading}
              />
            </div>

            <div className="form-group">
              <div className="slider-label-row">
                <label className="field-label">Vegetation Density</label>
                <span className="slider-val">{Math.round((config.vegetationDensity ?? 0.5) * 100)}%</span>
              </div>
              <input
                type="range"
                min="0.0"
                max="1.0"
                step="0.01"
                value={config.vegetationDensity ?? 0.5}
                onChange={(e) => setConfig((prev) => ({ ...prev, vegetationDensity: parseFloat(e.target.value) }))}
                className="slider-input"
                disabled={isLoading}
              />
            </div>

            <div className="grid-2col">
              <div className="form-group">
                <label className="field-label">Herbivores</label>
                <input
                  type="number"
                  min="0"
                  max="1000"
                  value={config.initialHerbivores ?? 50}
                  onChange={(e) => setConfig((prev) => ({ ...prev, initialHerbivores: Math.max(0, parseInt(e.target.value) || 0) }))}
                  className="text-input"
                  disabled={isLoading}
                />
              </div>

              <div className="form-group">
                <label className="field-label">Carnivores</label>
                <input
                  type="number"
                  min="0"
                  max="500"
                  value={config.initialCarnivores ?? 10}
                  onChange={(e) => setConfig((prev) => ({ ...prev, initialCarnivores: Math.max(0, parseInt(e.target.value) || 0) }))}
                  className="text-input"
                  disabled={isLoading}
                />
              </div>
            </div>
          </div>
        )}

        <button type="submit" className="btn-primary" disabled={isLoading}>
          <span>{isLoading ? 'Generating world' : 'Generate world'}</span>
          {!isLoading && (
            <svg viewBox="0 0 20 20" aria-hidden="true">
              <path d="m7 4 6 6-6 6" />
            </svg>
          )}
        </button>
      </form>

      {fingerprint && (
        <div className="fingerprint-card">
          <div className="card-title"><span className="verified-dot" />Canonical fingerprint</div>
          <code className="fingerprint-hash">{fingerprint}</code>
          <div className="fingerprint-hint">Deterministic state verified across runs</div>
        </div>
      )}

      <div className="legend-card">
        <div className="card-title">Map legend</div>
        <div className="legend-grid">
          <div className="legend-item"><span className="swatch deep-water"></span> Deep Water</div>
          <div className="legend-item"><span className="swatch shallow-water"></span> Shallow Water</div>
          <div className="legend-item"><span className="swatch sand"></span> Sand / Shore</div>
          <div className="legend-item"><span className="swatch grass"></span> Grassland</div>
          <div className="legend-item"><span className="swatch forest"></span> Forest</div>
          <div className="legend-item"><span className="swatch herbivore"></span> Herbivore</div>
          <div className="legend-item"><span className="swatch carnivore"></span> Carnivore</div>
        </div>
      </div>
    </aside>
  )
}
