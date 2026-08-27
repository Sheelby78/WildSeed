import type { Container } from 'pixi.js'

export class CameraController {
  private world: Container | null = null
  private canvas: HTMLCanvasElement | null = null
  private isDragging = false
  private dragStartX = 0
  private dragStartY = 0
  private cameraStartX = 0
  private cameraStartY = 0
  private zoom = 1.0
  private framing: 'contain' | 'cover' | null = null
  private worldSize = { width: 0, height: 0 }
  private viewSize = { width: 0, height: 0 }

  private onPointerDown = (e: PointerEvent) => {
    if (e.button !== 0 && e.button !== 1) return
    this.isDragging = true
    this.framing = null
    this.dragStartX = e.clientX
    this.dragStartY = e.clientY
    if (this.world) {
      this.cameraStartX = this.world.position.x
      this.cameraStartY = this.world.position.y
    }
  }

  private onPointerMove = (e: PointerEvent) => {
    if (!this.isDragging || !this.world) return
    const dx = e.clientX - this.dragStartX
    const dy = e.clientY - this.dragStartY
    this.world.position.x = this.cameraStartX + dx
    this.world.position.y = this.cameraStartY + dy
  }

  private onPointerUp = () => {
    this.isDragging = false
  }

  private onWheel = (e: WheelEvent) => {
    e.preventDefault()
    if (!this.world || !this.canvas) return

    const rect = this.canvas.getBoundingClientRect()
    const mouseX = e.clientX - rect.left
    const mouseY = e.clientY - rect.top

    const zoomFactor = e.deltaY < 0 ? 1.15 : 0.85
    this.zoomBy(zoomFactor, mouseX, mouseY)
  }

  zoomBy(factor: number, mouseX: number, mouseY: number): void {
    if (!this.world) return
    const newZoom = Math.max(0.1, Math.min(6.0, this.zoom * factor))

    if (newZoom === this.zoom) return

    this.framing = null

    const worldMouseX = (mouseX - this.world.position.x) / this.zoom
    const worldMouseY = (mouseY - this.world.position.y) / this.zoom

    this.zoom = newZoom
    this.world.scale.set(this.zoom)

    this.world.position.x = mouseX - worldMouseX * this.zoom
    this.world.position.y = mouseY - worldMouseY * this.zoom
  }

  resize(width: number, height: number): void {
    if (!this.world) return
    if (this.framing) {
      this.reset(this.worldSize.width, this.worldSize.height, width, height, this.framing)
      return
    }
    this.world.position.x += (width - this.viewSize.width) / 2
    this.world.position.y += (height - this.viewSize.height) / 2
    this.viewSize = { width, height }
  }

  attach(worldContainer: Container, canvas: HTMLCanvasElement): void {
    this.world = worldContainer
    this.canvas = canvas

    canvas.addEventListener('pointerdown', this.onPointerDown)
    window.addEventListener('pointermove', this.onPointerMove)
    window.addEventListener('pointerup', this.onPointerUp)
    window.addEventListener('pointercancel', this.onPointerUp)
    canvas.addEventListener('wheel', this.onWheel, { passive: false })
  }

  reset(worldWidth: number, worldHeight: number, viewWidth: number, viewHeight: number, framing: 'contain' | 'cover' = 'contain'): void {
    if (!this.world) return
    this.framing = framing
    this.worldSize = { width: worldWidth, height: worldHeight }
    this.viewSize = { width: viewWidth, height: viewHeight }
    const scaleX = viewWidth / worldWidth
    const scaleY = viewHeight / worldHeight
    this.zoom = framing === 'cover' ? Math.max(scaleX, scaleY) : Math.min(scaleX, scaleY)
    this.world.scale.set(this.zoom)
    this.world.position.x = (viewWidth - worldWidth * this.zoom) / 2
    this.world.position.y = (viewHeight - worldHeight * this.zoom) / 2
  }

  detach(): void {
    if (this.canvas) {
      this.canvas.removeEventListener('pointerdown', this.onPointerDown)
      this.canvas.removeEventListener('wheel', this.onWheel)
    }
    window.removeEventListener('pointermove', this.onPointerMove)
    window.removeEventListener('pointerup', this.onPointerUp)
    window.removeEventListener('pointercancel', this.onPointerUp)
    this.world = null
    this.canvas = null
  }
}
