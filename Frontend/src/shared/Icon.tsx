import type { CSSProperties } from 'react'

const paths = {
  seed: 'M12 21V11M12 15C6 15 3 11 3 5c6 0 9 4 9 10Zm0-4c0-6 3-9 9-9 0 6-3 9-9 9Z',
  play: 'm8 5 11 7-11 7V5Z',
  pause: 'M8 5v14M16 5v14',
  chart: 'M4 4v16h16M8 15l4-5 4 2 5-7',
  sliders: 'M4 7h9m4 0h3M4 17h3m4 0h9M13 4v6M7 14v6',
  globe: 'M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0ZM3 12h18M12 3c5 5 5 13 0 18-5-5-5-13 0-18Z',
  arrow: 'M4 12h16m-6-6 6 6-6 6',
  chevron: 'm9 5 7 7-7 7',
  shuffle: 'm16 3 4 4-4 4M4 7h3c4 0 6 10 10 10h3m-4-4 4 4-4 4M4 17h3c1 0 2-1 3-3m4-4c1-2 2-3 3-3h3',
  close: 'm6 6 12 12M6 18 18 6',
  fit: 'M9 3H3v6m12-6h6v6M3 15v6h6m12-6v6h-6',
  plus: 'M12 5v14M5 12h14',
  minus: 'M5 12h14',
  activity: 'M2 12h5l3-8 4 16 3-8h5',
  layers: 'm12 3 10 6-10 6L2 9l10-6Zm-10 12 10 6 10-6M2 12l10 6 10-6',
  hash: 'm10 3-4 18M18 3l-4 18M3 9h18M2 15h18',
} as const

export function Icon({ name, size = 18, style }: { name: keyof typeof paths, size?: number, style?: CSSProperties }) {
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" style={style}><path d={paths[name]} /></svg>
}
