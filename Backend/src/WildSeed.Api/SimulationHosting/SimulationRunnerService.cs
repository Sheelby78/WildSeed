using Microsoft.AspNetCore.SignalR;
using WildSeed.Api.Hubs;

namespace WildSeed.Api.SimulationHosting;

public sealed class SimulationRunnerService(SimulationSessionManager sessions, IHubContext<SimulationHub, ISimulationClient> hub) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var session in sessions.GetAll())
            {
                int ticks = session.Speed switch { "5x" => 5, "20x" => 20, "MAX" => 100, _ => 1 };
                for (int i = 0; i < ticks; i++) session.Advance();
                if (session.IsRunning) await hub.Clients.Group(session.Token).Snapshot(session.CreateResponse());
            }
            await Task.Delay(100, stoppingToken);
        }
    }
}
