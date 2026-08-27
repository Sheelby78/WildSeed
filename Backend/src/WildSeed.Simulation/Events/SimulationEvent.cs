namespace WildSeed.Simulation.Events;

public abstract record SimulationEvent(long Tick, Guid OrganismId);
