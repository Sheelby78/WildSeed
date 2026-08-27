using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;

namespace WildSeed.Simulation.Engine;

public static class SimulationStateFactory
{
    public static SimulationState Create(WorldMap world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var vegetation = new VegetationResource[world.Width * world.Height];
        for (int y = 0; y < world.Height; y++)
        for (int x = 0; x < world.Width; x++)
        {
            var tile = world.Tiles[x, y];
            int capacity = (int)MathF.Round(tile.VegetationDensity * SurvivalRulesV2.VegetationCapacityPerDensity, MidpointRounding.AwayFromZero);
            vegetation[y * world.Width + x] = new VegetationResource(capacity, capacity);
        }

        var organisms = world.Organisms
            .Where(organism => organism.IsAlive)
            .OrderBy(organism => organism.Id)
            .Select(organism => new OrganismState(organism.Id, organism.Species, organism.Genome, organism.X, organism.Y))
            .ToList();
        return new SimulationState(world, vegetation, organisms);
    }
}
