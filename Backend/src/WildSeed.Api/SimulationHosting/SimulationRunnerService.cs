using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using WildSeed.Api.Hubs;

namespace WildSeed.Api.SimulationHosting;

public sealed class SimulationRunnerService(SimulationSessionManager sessions, IHubContext<SimulationHub, ISimulationClient> hub) : BackgroundService
{
    private readonly ConcurrentDictionary<string, LatestSnapshotMailbox> _mailboxes = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var allSessions = sessions.GetAll().ToList();
            var activeTokens = allSessions.Select(s => s.Token).ToHashSet();

            foreach (var key in _mailboxes.Keys)
            {
                if (!activeTokens.Contains(key) && _mailboxes.TryRemove(key, out var mailbox))
                {
                    await mailbox.DisposeAsync();
                }
            }

            foreach (var session in allSessions)
            {
                if (!session.IsRunning) continue;

                int ticks = session.Speed switch { "5x" => 5, "20x" => 20, "MAX" => 100, _ => 1 };
                for (int i = 0; i < ticks; i++)
                {
                    session.Advance();
                }

                var mailbox = _mailboxes.GetOrAdd(session.Token, token => new LatestSnapshotMailbox(token, hub));
                mailbox.Post(session.CreateResponse());
            }

            await Task.Delay(100, stoppingToken);
        }

        foreach (var mailbox in _mailboxes.Values)
        {
            await mailbox.DisposeAsync();
        }
        _mailboxes.Clear();
    }
}
