using WildSeed.Simulation.Events;

namespace WildSeed.Simulation.Engine;

public sealed record SimulationTickResult(SimulationSnapshot Snapshot, IReadOnlyList<SimulationEvent> Events);
