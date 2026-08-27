using WildSeed.Domain.Organisms;

namespace WildSeed.Simulation.Engine;

public sealed record SimulationSnapshot(long Tick, IReadOnlyList<SimulationOrganismSnapshot> Organisms);

public sealed record SimulationOrganismSnapshot(Guid Id, Species Species, float X, float Y, int AgeTicks, OrganismNeeds Needs, OrganismAction Action);
