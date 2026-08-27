using WildSeed.Domain.Organisms;

namespace WildSeed.Simulation.Events;

public sealed record OrganismDied(long Tick, Guid OrganismId, Species Species, DeathCause Cause, float X, float Y)
    : SimulationEvent(Tick, OrganismId);
