export interface WorldConfig {
  seed?: number
  width?: number
  height?: number
  initialHerbivores?: number
  initialCarnivores?: number
  vegetationDensity?: number
  waterLevel?: number
  mutationProbability?: number
  mutationStrength?: number
}

export interface TileData {
  x: number
  y: number
  terrain: 'DeepWater' | 'ShallowWater' | 'Sand' | 'Grass' | 'Forest'
  vegetationDensity: number
}

export interface OrganismData {
  id: string
  species: 'Herbivore' | 'Carnivore'
  x: number
  y: number
  speed: number
}

export interface WorldSnapshot {
  width: number
  height: number
  tiles: TileData[][]
  organisms: OrganismData[]
  fingerprint: string
}

export interface RuntimeOrganism {
  id: string
  species: 'Herbivore' | 'Carnivore'
  x: number
  y: number
  action: string
}

export interface SimulationSnapshot {
  tick: number
  isRunning: boolean
  speed: string
  fingerprint: string
  population: number
  herbivores: number
  carnivores: number
  actions: Record<string, number>
  deaths: Record<string, number>
  organisms: RuntimeOrganism[]
}

export interface GeneratedWorld {
  sessionToken: string
  staticWorld: WorldSnapshot
  snapshot: SimulationSnapshot
}

export async function generateWorld(config: WorldConfig): Promise<GeneratedWorld> {
  const response = await fetch('/api/world/generate', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(config),
  })

  if (!response.ok) {
    const errorBody = await response.json().catch(() => ({}))
    throw new Error(errorBody.detail || `World generation failed with status ${response.status}`)
  }

  return response.json()
}
