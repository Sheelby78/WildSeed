using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Behavior;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;

namespace WildSeed.Simulation.Lifecycle;

public static class PredationResolver
{
    public static IReadOnlyList<OrganismDied> Resolve(SimulationState state, ReadOnlySpan<(OrganismState Organism, ActionIntent Intent)> scored)
    {
        var deadSet = new HashSet<Guid>();
        var deaths = new List<OrganismDied>();

        var organismMap = new Dictionary<Guid, OrganismState>(state.Organisms.Count);
        foreach (var org in state.Organisms)
        {
            organismMap[org.Id] = org;
        }

        foreach (var (attacker, intent) in scored)
        {
            if (intent.Action == OrganismAction.Attack && intent.TargetOrganismId is { } targetId)
            {
                if (organismMap.TryGetValue(targetId, out var targetPrey))
                {
                    attacker.Needs = attacker.Needs.Feed(SurvivalRulesV4.PredationHungerGain, SurvivalRulesV4.PredationEnergyGain);

                    if (deadSet.Add(targetId))
                    {
                        deaths.Add(new OrganismDied(state.Tick, targetPrey.Id, targetPrey.Species, DeathCause.Predation, targetPrey.X, targetPrey.Y, targetPrey.AgeTicks));
                    }
                }
            }
        }

        if (deadSet.Count > 0)
        {
            state.Organisms.RemoveAll(org => deadSet.Contains(org.Id));
        }

        return deaths;
    }
}
