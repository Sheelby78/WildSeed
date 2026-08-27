using WildSeed.Api.Contracts;
using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Determinism;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;

namespace WildSeed.Api.SimulationHosting;

public sealed class SimulationSession
{
    private readonly object _gate = new();
    private readonly SimulationEngine _engine;
    private string? _ownerConnectionId;
    private DateTimeOffset? _detachedAt;
    private readonly Dictionary<string, int> _deathCounts = [];
    private string _cachedFingerprint = "";
    private long _lastFingerprintTick = -1;

    public SimulationSession(string token, SimulationState state)
    {
        Token = token;
        _engine = new SimulationEngine(state);
    }

    public string Token { get; }
    public bool IsRunning { get; private set; }
    public string Speed { get; private set; } = "1x";
    public DateTimeOffset? DetachedAt { get { lock (_gate) return _detachedAt; } }
    public SimulationSnapshotResponse CreateResponse()
    {
        lock (_gate)
        {
            var snapshot = _engine.Snapshot();
            if (string.IsNullOrEmpty(_cachedFingerprint) || snapshot.Tick - _lastFingerprintTick >= 10 || !IsRunning)
            {
                _cachedFingerprint = SimulationStateFingerprint.Compute(GetState()).ToString();
                _lastFingerprintTick = snapshot.Tick;
            }
            int herbivores = snapshot.Organisms.Count(item => item.Species == Species.Herbivore);
            int carnivores = snapshot.Organisms.Count(item => item.Species == Species.Carnivore);
            return new SimulationSnapshotResponse(snapshot.Tick, IsRunning, Speed, _cachedFingerprint, snapshot.Organisms.Count,
                herbivores, carnivores,
                snapshot.Organisms.GroupBy(item => item.Action.ToString()).ToDictionary(group => group.Key, group => group.Count()), new Dictionary<string, int>(_deathCounts), snapshot.Organisms.Select(item => new RuntimeOrganismDto(item.Id.ToString(), item.Species.ToString(), item.X, item.Y, item.Action.ToString())).ToArray());
        }
    }
    public SimulationStatusResponse Status() { var snapshot = CreateResponse(); return new SimulationStatusResponse(snapshot.Tick, snapshot.IsRunning, snapshot.Speed, snapshot.Fingerprint, snapshot.Population); }
    public bool Attach(string connectionId) { lock (_gate) { if (_ownerConnectionId is not null && _ownerConnectionId != connectionId) return false; _ownerConnectionId = connectionId; _detachedAt = null; IsRunning = false; return true; } }
    public void Detach(string connectionId, TimeProvider timeProvider) { lock (_gate) if (_ownerConnectionId == connectionId) { _ownerConnectionId = null; _detachedAt = timeProvider.GetUtcNow(); IsRunning = false; } }
    public void Start(string speed) { lock (_gate) { Speed = speed; IsRunning = true; } }
    public void Pause() { lock (_gate) IsRunning = false; }
    public SimulationTickResult? Advance()
    {
        lock (_gate)
        {
            if (!IsRunning) return null;
            var result = _engine.AdvanceTick();
            foreach (var death in result.Events.OfType<OrganismDied>())
            {
                string cause = death.Cause.ToString();
                _deathCounts[cause] = _deathCounts.GetValueOrDefault(cause) + 1;
            }
            return result;
        }
    }
    public SimulationState GetState() => _engine.State;
}
