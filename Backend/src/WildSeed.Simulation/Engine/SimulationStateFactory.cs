using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;

namespace WildSeed.Simulation.Engine;

public static class SimulationStateFactory
{
    private static readonly (int Dx, int Dy)[] CardinalOffsets = [(0, -1), (-1, 0), (1, 0), (0, 1)];

    public static SimulationState Create(WorldMap world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var vegetation = new VegetationResource[world.Width * world.Height];
        var drinkingTiles = new bool[world.Width * world.Height];

        for (int y = 0; y < world.Height; y++)
        for (int x = 0; x < world.Width; x++)
        {
            var tile = world.Tiles[x, y];
            int capacity = (int)MathF.Round(tile.VegetationDensity * SurvivalRulesV2.VegetationCapacityPerDensity, MidpointRounding.AwayFromZero);
            vegetation[y * world.Width + x] = new VegetationResource(capacity, capacity);

            if (tile.Terrain is not (TerrainType.DeepWater or TerrainType.ShallowWater))
            {
                bool isDrinking = false;
                foreach (var (dx, dy) in CardinalOffsets)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < world.Width && ny < world.Height &&
                        world.Tiles[nx, ny].Terrain is TerrainType.DeepWater or TerrainType.ShallowWater)
                    {
                        isDrinking = true;
                        break;
                    }
                }
                drinkingTiles[y * world.Width + x] = isDrinking;
            }
        }

        var organisms = world.Organisms
            .Where(organism => organism.IsAlive)
            .OrderBy(organism => organism.Id)
            .Select(organism => new OrganismState(organism.Id, organism.Species, organism.Genome, organism.X, organism.Y, organism.MotherId, organism.FatherId, organism.Generation))
            .ToList();
        return new SimulationState(world, vegetation, organisms, drinkingTiles);
    }
}
