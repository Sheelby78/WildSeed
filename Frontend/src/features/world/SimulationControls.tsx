interface SimulationControlsProps {
  running: boolean
  speed: string
  disabled: boolean
  onToggle: () => void
  onSpeed: (speed: string) => void
}

export function SimulationControls({ running, speed, disabled, onToggle, onSpeed }: SimulationControlsProps) {
  return <div className="simulation-controls">
    <button type="button" className="btn-primary" disabled={disabled} onClick={onToggle}>{running ? 'Pause simulation' : 'Start simulation'}</button>
    <div className="speed-buttons">
      {['1x', '5x', '20x', 'MAX'].map(option => <button type="button" key={option} disabled={disabled} onClick={() => onSpeed(option)} className={speed === option ? 'speed-active' : ''}>{option}</button>)}
    </div>
  </div>
}
