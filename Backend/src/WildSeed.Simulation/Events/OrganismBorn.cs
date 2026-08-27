using WildSeed.Domain.Organisms;

namespace WildSeed.Simulation.Events;

public sealed record OrganismBorn(
    long Tick,
    Guid OrganismId,
    Species Species,
    float X,
    float Y,
    Guid? MotherId,
    Guid? FatherId,
    int Generation,
    Genome Genome) : SimulationEvent(Tick, OrganismId);
