using WildSeed.Simulation.Engine;

namespace WildSeed.Api.Contracts;

public sealed record SimulationSnapshotResponse(long Tick, bool IsRunning, string Speed, string Fingerprint, int Population, IReadOnlyDictionary<string, int> Actions, IReadOnlyDictionary<string, int> Deaths, IReadOnlyList<RuntimeOrganismDto> Organisms)
{
    public static SimulationSnapshotResponse FromState(SimulationHosting.SimulationSession session) => session.CreateResponse();
}

public sealed record RuntimeOrganismDto(string Id, string Species, float X, float Y, string Action);
