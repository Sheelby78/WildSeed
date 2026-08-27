using Microsoft.AspNetCore.SignalR;
using WildSeed.Api.Contracts;
using WildSeed.Api.SimulationHosting;

namespace WildSeed.Api.Hubs;

public sealed class SimulationHub(SimulationSessionManager sessions, TimeProvider timeProvider) : Hub<ISimulationClient>
{
    public async Task<SimulationCommandResult> Attach(string token)
    {
        if (!sessions.TryGet(token, out var session) || session is null || !session.Attach(Context.ConnectionId)) return new SimulationCommandResult(false, "session-unavailable", null);
        await Groups.AddToGroupAsync(Context.ConnectionId, token);
        return new SimulationCommandResult(true, null, session.Status());
    }
    public Task<SimulationCommandResult> Start(string token, string speed) => Command(token, session => session.Start(speed));
    public Task<SimulationCommandResult> Pause(string token) => Command(token, session => session.Pause());
    public override Task OnDisconnectedAsync(Exception? exception) { foreach (var token in Context.GetHttpContext()?.Request.Query["session"].ToArray().Where(item => item is not null) ?? []) if (sessions.TryGet(token!, out var session) && session is not null) session.Detach(Context.ConnectionId, timeProvider); return base.OnDisconnectedAsync(exception); }
    private Task<SimulationCommandResult> Command(string token, Action<SimulationSession> command) { if (!sessions.TryGet(token, out var session) || session is null) return Task.FromResult(new SimulationCommandResult(false, "session-unavailable", null)); command(session); return Task.FromResult(new SimulationCommandResult(true, null, session.Status())); }
}
