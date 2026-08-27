using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;

namespace WildSeed.Simulation.Lifecycle;

public static class DeathResolver
{
    public static IReadOnlyList<SimulationEvent> Resolve(SimulationState state)
    {
        var dead = new List<OrganismDied>();
        foreach (var organism in state.Organisms)
        {
            int maxAge = organism.Species == Species.Carnivore
                ? SurvivalRulesV4.CarnivoreMaximumAgeTicks
                : SurvivalRulesV4.HerbivoreMaximumAgeTicks;

            DeathCause? cause = organism.Needs.Thirst >= OrganismNeeds.Maximum
                ? DeathCause.Dehydration
                : organism.Needs.Hunger >= OrganismNeeds.Maximum
                    ? DeathCause.Starvation
                    : organism.AgeTicks >= maxAge
                        ? DeathCause.OldAge
                        : null;

            if (cause is not null)
            {
                dead.Add(new OrganismDied(state.Tick, organism.Id, organism.Species, cause.Value, organism.X, organism.Y, organism.AgeTicks));
            }
        }

        if (dead.Count > 0)
        {
            state.Organisms.RemoveAll(organism => dead.Any(item => item.OrganismId == organism.Id));
        }

        return dead.OrderBy(item => item.OrganismId).Cast<SimulationEvent>().ToArray();
    }
}
