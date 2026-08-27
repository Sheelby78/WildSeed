using WildSeed.Domain.Terrain;
using WildSeed.Simulation.Behavior;
using WildSeed.Simulation.Engine;

namespace WildSeed.Simulation.Resources;

public sealed class VegetationResolver
{
    public void Resolve(SimulationState state, IEnumerable<ActionIntent> intents)
    {
        var organismById = state.Organisms.ToDictionary(organism => organism.Id);
        foreach (var group in intents.Where(intent => intent.Action == Domain.Organisms.OrganismAction.Eat && intent.Target is not null).GroupBy(intent => intent.Target!.Value).OrderBy(group => group.Key.Y).ThenBy(group => group.Key.X))
        {
            int index = group.Key.Y * state.World.Width + group.Key.X;
            var resource = state.Vegetation[index];
            var grants = ProportionalResourceAllocator.Allocate(resource.Current, group.Select(intent => (intent.OrganismId, 50)));
            int consumed = 0;
            foreach (var (id, grant) in grants)
            {
                organismById[id].Needs = organismById[id].Needs.Eat(grant * Contracts.SurvivalRulesV2.FoodNeedPerVegetationUnit);
                consumed += grant;
            }
            state.Vegetation[index] = resource.Consume(consumed);
        }
    }
}
