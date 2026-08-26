using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Determinism;

namespace WildSeed.Simulation.Tests.Fixtures;

public sealed class DeterministicProbe
{
    private readonly ulong _seed;
    private readonly int _contractVersion;
    private readonly List<ProbeAgent> _agents;
    private ulong _rngState;
    private long _tick;

    public DeterministicProbe(ulong seed, int initialPopulation = 5, int contractVersion = SimulationContract.Version)
    {
        _seed = seed;
        _contractVersion = contractVersion;
        _rngState = seed == 0 ? 0x853c49e6748fea9bUL : seed;
        _tick = 0;
        _agents = new List<ProbeAgent>(initialPopulation);

        for (int i = 1; i <= initialPopulation; i++)
        {
            int x = (int)(NextRandom() % 1000);
            int y = (int)(NextRandom() % 1000);
            int energy = 50 + (int)(NextRandom() % 50);
            _agents.Add(new ProbeAgent(i, x, y, energy));
        }
    }

    public long CurrentTick => _tick;
    public ulong Seed => _seed;
    public int ContractVersion => _contractVersion;

    public DeterministicProbeState GetState()
    {
        return new DeterministicProbeState(_seed, _contractVersion, _tick, _agents.ToArray());
    }

    public void AdvanceTicks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            AdvanceTick();
        }
    }

    public void AdvanceTick()
    {
        _tick++;
        for (int i = 0; i < _agents.Count; i++)
        {
            var agent = _agents[i];
            int dx = (int)(NextRandom() % 7) - 3;
            int dy = (int)(NextRandom() % 7) - 3;
            int energyChange = (int)(NextRandom() % 3) - 1;

            int newX = Math.Clamp(agent.X + dx, 0, 1000);
            int newY = Math.Clamp(agent.Y + dy, 0, 1000);
            int newEnergy = Math.Clamp(agent.Energy + energyChange, 0, 100);

            _agents[i] = new ProbeAgent(agent.Id, newX, newY, newEnergy);
        }
    }

    public StateFingerprint ComputeFingerprint()
    {
        using var writer = new CanonicalStateWriter();
        writer.WriteHeader(_seed, _tick, _contractVersion);
        writer.WriteOrdered(_agents, a => a.Id, (w, a) =>
        {
            w.WriteInt32(a.Id);
            w.WriteInt32(a.X);
            w.WriteInt32(a.Y);
            w.WriteInt32(a.Energy);
        });

        return StateFingerprint.Compute(writer, _contractVersion);
    }

    private ulong NextRandom()
    {
        _rngState ^= _rngState >> 12;
        _rngState ^= _rngState << 25;
        _rngState ^= _rngState >> 27;
        return _rngState * 0x2545F4914F6CDD1DUL;
    }
}
