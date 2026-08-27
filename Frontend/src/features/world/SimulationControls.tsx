import { Icon } from '@/shared/Icon'

interface SimulationControlsProps {
  running: boolean
  speed: string
  disabled: boolean
  onToggle: () => void
  onSpeed: (speed: string) => void
}

export function SimulationControls({ running, speed, disabled, onToggle, onSpeed }: SimulationControlsProps) {
  return <div className="simulation-controls" aria-label="Simulation controls">
    <button type="button" className={`btn-primary playback-button ${running ? 'is-running' : ''}`} disabled={disabled} onClick={onToggle}>
      <Icon name={running ? 'pause' : 'play'} size={16} />
      {running ? 'Pause simulation' : 'Start simulation'}
    </button>
    <div className="speed-control">
      <span className="control-label">Speed</span>
      <div className="speed-buttons" role="group" aria-label="Simulation speed">
        {['1x', '5x', '20x', 'MAX'].map(option => <button type="button" key={option} disabled={disabled} onClick={() => onSpeed(option)} aria-pressed={speed === option} className={speed === option ? 'speed-active' : ''}>{option}</button>)}
      </div>
    </div>
  </div>
}
