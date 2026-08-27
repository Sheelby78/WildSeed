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

export class WorldRenderer {
  private app: Application | null = null
  private worldContainer: Container | null = null
  private terrainGraphics: Graphics | null = null
  private organismGraphics: Graphics | null = null
  private camera = new CameraController()
  private containerElement: HTMLElement | null = null
  private isDestroyed = false

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
    }

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

    if (snapshot.organisms.length > 0) {
      this.organismGraphics = new Graphics()
      const orgRadius = Math.max(2, TILE_SIZE * 0.25)

      for (const org of snapshot.organisms) {
        const color = org.species === 'Herbivore' ? 0xfacc15 : 0xef4444
        this.organismGraphics.circle(org.x * TILE_SIZE, org.y * TILE_SIZE, orgRadius)
        this.organismGraphics.fill({ color })
      }

      this.worldContainer.addChild(this.organismGraphics)
    }

    const worldWidthPx = snapshot.width * TILE_SIZE
    const worldHeightPx = snapshot.height * TILE_SIZE
    const viewWidth = this.app.screen.width
    const viewHeight = this.app.screen.height

    this.camera.reset(worldWidthPx, worldHeightPx, viewWidth, viewHeight)
  }

  updateOrganisms(organisms: RuntimeOrganism[]): void {
    if (!this.worldContainer || this.isDestroyed) return
    if (this.organismGraphics) {
      this.worldContainer.removeChild(this.organismGraphics)
      this.organismGraphics.destroy()
    }
    this.organismGraphics = new Graphics()
    for (const organism of organisms) {
      this.organismGraphics.circle(organism.x * TILE_SIZE, organism.y * TILE_SIZE, Math.max(2, TILE_SIZE * 0.25))
      this.organismGraphics.fill({ color: organism.species === 'Herbivore' ? 0xfacc15 : 0xef4444 })
    }
    this.worldContainer.addChild(this.organismGraphics)
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
