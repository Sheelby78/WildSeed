using WildSeed.Domain.Organisms;

namespace WildSeed.Simulation.Behavior;

public readonly record struct ActionIntent(Guid OrganismId, OrganismAction Action, (int X, int Y)? Target, Guid? TargetOrganismId = null);
