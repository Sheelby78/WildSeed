import * as signalR from '@microsoft/signalr'
import type { SimulationSnapshot } from './WorldApi'

export class SimulationConnection {
  private readonly connection: signalR.HubConnection
  private readonly token: string

  constructor(token: string, onSnapshot: (snapshot: SimulationSnapshot) => void) {
    this.token = token
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`/hubs/simulation?session=${encodeURIComponent(token)}`)
      .withAutomaticReconnect([0, 1000, 3000, 5000])
      .build()
    this.connection.on('Snapshot', onSnapshot)
    this.connection.onreconnected(() => this.attach())
  }

  async start(): Promise<void> {
    await this.connection.start()
    await this.attach()
  }

  async command(isRunning: boolean, speed = '1x'): Promise<void> {
    if (isRunning) await this.connection.invoke('Start', this.token, speed)
    else await this.connection.invoke('Pause', this.token)
  }

  async stop(): Promise<void> {
    await this.connection.stop()
  }

  private async attach(): Promise<void> {
    await this.connection.invoke('Attach', this.token)
  }
}
