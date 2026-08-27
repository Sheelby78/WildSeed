import { Application, Container, Graphics } from 'pixi.js'
import type { RuntimeOrganism, TileData, WorldSnapshot } from '@/transport/WorldApi'
import { CameraController } from './CameraController'

const TILE_SIZE = 8

function getTerrainColor(terrain: TileData['terrain'], vegetation: number): number {
  switch (terrain) {
    case 'DeepWater':
      return 0x162a45
    case 'ShallowWater':
      return 0x2563eb
    case 'Sand':
      return 0xd97706
    case 'Grass': {
      const g = Math.floor(130 + vegetation * 70)
      const r = Math.floor(60 + (1 - vegetation) * 40)
      return (r << 16) | (g << 8) | 40
    }
    case 'Forest': {
      const g = Math.floor(90 + vegetation * 50)
      const r = Math.floor(25 + (1 - vegetation) * 20)
      return (r << 16) | (g << 8) | 25
    }
  }
}

interface RenderOrganism {
  id: string
  species: 'Herbivore' | 'Carnivore'
  currentX: number
  currentY: number
  targetX: number
  targetY: number
  action: string
  genomeSize: number
}

export class WorldRenderer {
  private app: Application | null = null
  private worldContainer: Container | null = null
  private terrainGraphics: Graphics | null = null
  private organismGraphics: Graphics | null = null
  private camera = new CameraController()
  private containerElement: HTMLElement | null = null
  private isDestroyed = false
  private organismsMap = new Map<string, RenderOrganism>()

  async init(container: HTMLElement): Promise<void> {
    this.containerElement = container

    const app = new Application()

    const initialWidth = container.clientWidth || window.innerWidth - 356
    const initialHeight = container.clientHeight || window.innerHeight

    await app.init({
      width: Math.max(100, initialWidth),
      height: Math.max(100, initialHeight),
      backgroundColor: 0x090d16,
      antialias: false,
      resolution: window.devicePixelRatio || 1,
      autoDensity: true,
      preference: 'webgl',
    })

    if (this.isDestroyed) {
      try {
        app.destroy(true, { children: true, texture: false })
      } catch {
        // Safe disposal
      }
      return
    }

    this.app = app
    app.canvas.className = 'viewport-canvas'
    container.appendChild(app.canvas)

    this.worldContainer = new Container()
    this.app.stage.addChild(this.worldContainer)

    this.camera.attach(this.worldContainer, app.canvas)

    this.app.ticker.add(ticker => {
      this.renderFrame(ticker.deltaTime)
    })
  }

  renderWorld(snapshot: WorldSnapshot): void {
    if (!this.worldContainer || !this.app || this.isDestroyed) return

    if (this.terrainGraphics) {
      this.worldContainer.removeChild(this.terrainGraphics)
      this.terrainGraphics.destroy()
    }
    if (this.organismGraphics) {
      this.worldContainer.removeChild(this.organismGraphics)
      this.organismGraphics.destroy()
      this.organismGraphics = null
    }

    this.organismsMap.clear()

    this.terrainGraphics = new Graphics()

    for (let y = 0; y < snapshot.height; y++) {
      for (let x = 0; x < snapshot.width; x++) {
        const tile = snapshot.tiles[y][x]
        const color = getTerrainColor(tile.terrain, tile.vegetationDensity)
        this.terrainGraphics.rect(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE)
        this.terrainGraphics.fill({ color })
      }
    }

    this.worldContainer.addChild(this.terrainGraphics)

    for (const org of snapshot.organisms) {
      this.organismsMap.set(org.id, {
        id: org.id,
        species: org.species,
        currentX: org.x,
        currentY: org.y,
        targetX: org.x,
        targetY: org.y,
        action: 'Explore',
        genomeSize: org.genome?.size ?? 1.0,
      })
    }

    const worldWidthPx = snapshot.width * TILE_SIZE
    const worldHeightPx = snapshot.height * TILE_SIZE
    const viewWidth = this.app.screen.width
    const viewHeight = this.app.screen.height

    this.camera.reset(worldWidthPx, worldHeightPx, viewWidth, viewHeight)
  }

  updateOrganisms(organisms: RuntimeOrganism[]): void {
    if (!this.worldContainer || this.isDestroyed) return

    const incomingIds = new Set<string>()

    for (const incoming of organisms) {
      incomingIds.add(incoming.id)
      const existing = this.organismsMap.get(incoming.id)
      const genomeSize = incoming.genome?.size ?? 1.0

      if (existing) {
        existing.targetX = incoming.x
        existing.targetY = incoming.y
        existing.action = incoming.action
        existing.species = incoming.species
        existing.genomeSize = genomeSize
      } else {
        this.organismsMap.set(incoming.id, {
          id: incoming.id,
          species: incoming.species,
          currentX: incoming.x,
          currentY: incoming.y,
          targetX: incoming.x,
          targetY: incoming.y,
          action: incoming.action,
          genomeSize,
        })
      }
    }

    for (const id of this.organismsMap.keys()) {
      if (!incomingIds.has(id)) {
        this.organismsMap.delete(id)
      }
    }
  }

  private renderFrame(deltaTime: number): void {
    if (!this.worldContainer || this.isDestroyed || this.organismsMap.size === 0) return

    if (!this.organismGraphics) {
      this.organismGraphics = new Graphics()
      this.worldContainer.addChild(this.organismGraphics)
    }

    const g = this.organismGraphics
    g.clear()

    const lerpFactor = Math.min(1.0, deltaTime * 0.25)

    for (const organism of this.organismsMap.values()) {
      organism.currentX += (organism.targetX - organism.currentX) * lerpFactor
      organism.currentY += (organism.targetY - organism.currentY) * lerpFactor
    }

    let hasFlee = false
    let hasHunt = false
    let hasMate = false
    let hasHerb = false
    let hasCarn = false

    // Action rings
    for (const organism of this.organismsMap.values()) {
      if (organism.action === 'Flee') {
        const isCarnivore = organism.species === 'Carnivore'
        const baseRadius = Math.max(1.5, TILE_SIZE * (isCarnivore ? 0.32 : 0.25) * organism.genomeSize)
        g.circle(organism.currentX * TILE_SIZE, organism.currentY * TILE_SIZE, baseRadius + 2.5)
        hasFlee = true
      }
    }
    if (hasFlee) {
      g.stroke({ color: 0x38bdf8, width: 1.5, alpha: 0.85 })
    }

    for (const organism of this.organismsMap.values()) {
      if (organism.action === 'Hunt' || organism.action === 'Attack') {
        const isCarnivore = organism.species === 'Carnivore'
        const baseRadius = Math.max(1.5, TILE_SIZE * (isCarnivore ? 0.32 : 0.25) * organism.genomeSize)
        g.circle(organism.currentX * TILE_SIZE, organism.currentY * TILE_SIZE, baseRadius + 2.5)
        hasHunt = true
      }
    }
    if (hasHunt) {
      g.stroke({ color: 0xf97316, width: 1.5, alpha: 0.9 })
    }

    for (const organism of this.organismsMap.values()) {
      if (organism.action === 'Mate') {
        const isCarnivore = organism.species === 'Carnivore'
        const baseRadius = Math.max(1.5, TILE_SIZE * (isCarnivore ? 0.32 : 0.25) * organism.genomeSize)
        g.circle(organism.currentX * TILE_SIZE, organism.currentY * TILE_SIZE, baseRadius + 2.5)
        hasMate = true
      }
    }
    if (hasMate) {
      g.stroke({ color: 0xec4899, width: 1.5, alpha: 0.9 })
    }

    // Herbivore bodies
    for (const organism of this.organismsMap.values()) {
      if (organism.species === 'Herbivore') {
        const baseRadius = Math.max(1.5, TILE_SIZE * 0.25 * organism.genomeSize)
        g.circle(organism.currentX * TILE_SIZE, organism.currentY * TILE_SIZE, baseRadius)
        hasHerb = true
      }
    }
    if (hasHerb) {
      g.fill({ color: 0xfacc15 })
    }

    // Carnivore bodies
    for (const organism of this.organismsMap.values()) {
      if (organism.species === 'Carnivore') {
        const baseRadius = Math.max(1.5, TILE_SIZE * 0.32 * organism.genomeSize)
        g.circle(organism.currentX * TILE_SIZE, organism.currentY * TILE_SIZE, baseRadius)
        hasCarn = true
      }
    }
    if (hasCarn) {
      g.fill({ color: 0xef4444 })
    }
  }

  resize(): void {
    if (this.app && this.containerElement && !this.isDestroyed) {
      const width = this.containerElement.clientWidth
      const height = this.containerElement.clientHeight
      if (width > 0 && height > 0) {
        this.app.renderer.resize(width, height)
      }
    }
  }

  getFPS(): number {
    return this.app ? Math.round(this.app.ticker.FPS) : 0
  }

  destroy(): void {
    this.isDestroyed = true
    this.camera.detach()
    if (this.app) {
      if (this.app.canvas && this.containerElement?.contains(this.app.canvas)) {
        this.containerElement.removeChild(this.app.canvas)
      }
      try {
        this.app.destroy(true, { children: true, texture: false })
      } catch {
        // Safe disposal
      }
      this.app = null
    }
    this.worldContainer = null
    this.terrainGraphics = null
    this.organismGraphics = null
    this.containerElement = null
  }
}
