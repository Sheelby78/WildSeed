using System.Security.Cryptography;
using WildSeed.Simulation.Engine;

namespace WildSeed.Api.SimulationHosting;

public sealed class SimulationSessionManager
{
    private readonly Dictionary<string, SimulationSession> _sessions = [];
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly SimulationHostOptions _options;
    public SimulationSessionManager(TimeProvider timeProvider, SimulationHostOptions options) { _timeProvider = timeProvider; _options = options; }
    public SimulationSession Create(SimulationState state) { var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32)); var session = new SimulationSession(token, state); lock (_gate) _sessions[token] = session; return session; }
    public bool TryGet(string token, out SimulationSession? session) { lock (_gate) { Purge(); return _sessions.TryGetValue(token, out session); } }
    public IReadOnlyList<SimulationSession> GetAll() { lock (_gate) { Purge(); return _sessions.Values.ToArray(); } }
    private void Purge() { var cutoff = _timeProvider.GetUtcNow() - TimeSpan.FromSeconds(_options.RetentionSeconds); foreach (var token in _sessions.Where(pair => pair.Value.DetachedAt is { } detached && detached <= cutoff).Select(pair => pair.Key).ToArray()) _sessions.Remove(token); }
}
