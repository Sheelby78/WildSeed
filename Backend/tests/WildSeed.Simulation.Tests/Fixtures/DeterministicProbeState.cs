namespace WildSeed.Simulation.Tests.Fixtures;

public readonly record struct ProbeAgent(int Id, int X, int Y, int Energy);

public sealed class DeterministicProbeState
{
    public ulong Seed { get; }
    public int ContractVersion { get; }
    public long Tick { get; }
    public IReadOnlyList<ProbeAgent> Agents { get; }

    public DeterministicProbeState(ulong seed, int contractVersion, long tick, IReadOnlyList<ProbeAgent> agents)
    {
        Seed = seed;
        ContractVersion = contractVersion;
        Tick = tick;
        Agents = agents;
    }
}
