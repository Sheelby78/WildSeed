import type { WorldConfig } from '@/transport/WorldApi'

export interface WorldPreset {
  id: string
  name: string
  description: string
  config: WorldConfig
}

export const WORLD_PRESETS: WorldPreset[] = [
  {
    id: 'island',
    name: 'Archipelago / Island',
    description: 'Higher water levels forming isolated landmasses and islands.',
    config: {
      seed: 1337,
      width: 128,
      height: 128,
      waterLevel: 0.62,
      vegetationDensity: 0.65,
      initialHerbivores: 70,
      initialCarnivores: 12,
      mutationProbability: 0.30,
      mutationStrength: 0.25,
    },
  },
  {
    id: 'continental',
    name: 'Continental',
    description: 'Vast continuous landmasses with inland lakes and rich forests.',
    config: {
      seed: 4242,
      width: 192,
      height: 192,
      waterLevel: 0.38,
      vegetationDensity: 0.7,
      initialHerbivores: 150,
      initialCarnivores: 30,
      mutationProbability: 0.30,
      mutationStrength: 0.25,
    },
  },
  {
    id: 'arid',
    name: 'Arid Wasteland',
    description: 'Scarce water, sparse vegetation, harsh survival conditions.',
    config: {
      seed: 8888,
      width: 128,
      height: 128,
      waterLevel: 0.22,
      vegetationDensity: 0.2,
      initialHerbivores: 40,
      initialCarnivores: 8,
      mutationProbability: 0.35,
      mutationStrength: 0.30,
    },
  },
  {
    id: 'lush',
    name: 'Lush Basin',
    description: 'Dense vegetation, abundant rivers, high biodiversity.',
    config: {
      seed: 7777,
      width: 128,
      height: 128,
      waterLevel: 0.45,
      vegetationDensity: 0.95,
      initialHerbivores: 120,
      initialCarnivores: 25,
      mutationProbability: 0.30,
      mutationStrength: 0.25,
    },
  },
]
