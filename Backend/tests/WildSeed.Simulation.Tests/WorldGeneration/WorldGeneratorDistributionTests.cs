using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.WorldGeneration;
using Xunit;

namespace WildSeed.Simulation.Tests.WorldGeneration;

public sealed class WorldGeneratorDistributionTests
{
    [Fact]
    public void HigherWaterLevel_ProducesMoreWaterTiles()
    {
        var generator = new WorldGenerator();
        var lowWaterConfig = new WorldConfiguration(42, 128, 128, 0, 0, 0.5f, 0.2f, 0.05f, 0.1f);
        var highWaterConfig = new WorldConfiguration(42, 128, 128, 0, 0, 0.5f, 0.7f, 0.05f, 0.1f);

        var lowWaterWorld = generator.Generate(lowWaterConfig);
        var highWaterWorld = generator.Generate(highWaterConfig);

        int lowWaterCount = CountTiles(lowWaterWorld, t => t is TerrainType.DeepWater or TerrainType.ShallowWater);
        int highWaterCount = CountTiles(highWaterWorld, t => t is TerrainType.DeepWater or TerrainType.ShallowWater);

        Assert.True(highWaterCount > lowWaterCount, $"Expected high water ({highWaterCount}) to exceed low water ({lowWaterCount})");
    }

    [Fact]
    public void HigherVegetationDensity_ProducesHigherAverageLandVegetation()
    {
        var generator = new WorldGenerator();
        var sparseConfig = new WorldConfiguration(42, 128, 128, 0, 0, 0.2f, 0.4f, 0.05f, 0.1f);
        var lushConfig = new WorldConfiguration(42, 128, 128, 0, 0, 0.9f, 0.4f, 0.05f, 0.1f);

        var sparseWorld = generator.Generate(sparseConfig);
        var lushWorld = generator.Generate(lushConfig);

        float sparseAvg = AverageLandVegetation(sparseWorld);
        float lushAvg = AverageLandVegetation(lushWorld);

        Assert.True(lushAvg > sparseAvg, $"Expected lush avg ({lushAvg}) to exceed sparse avg ({sparseAvg})");
    }

    [Fact]
    public void Organisms_ArePlacedOnLandTilesOnly()
    {
        var generator = new WorldGenerator();
        var config = new WorldConfiguration(42, 128, 128, 100, 50, 0.5f, 0.4f, 0.05f, 0.1f);

        var world = generator.Generate(config);

        Assert.Equal(150, world.Organisms.Count);

        foreach (var organism in world.Organisms)
        {
            int tileX = (int)organism.X;
            int tileY = (int)organism.Y;

            var tile = world.GetTile(tileX, tileY);
            Assert.NotEqual(TerrainType.DeepWater, tile.Terrain);
            Assert.NotEqual(TerrainType.ShallowWater, tile.Terrain);
        }
    }

    [Fact]
    public void OrganismCounts_MatchConfiguration()
    {
        var generator = new WorldGenerator();
        var config = new WorldConfiguration(42, 128, 128, 75, 25, 0.5f, 0.4f, 0.05f, 0.1f);

        var world = generator.Generate(config);

        int herbivores = world.Organisms.Count(o => o.Species == Species.Herbivore);
        int carnivores = world.Organisms.Count(o => o.Species == Species.Carnivore);

        Assert.Equal(75, herbivores);
        Assert.Equal(25, carnivores);
    }

    private static int CountTiles(WorldMap world, Func<TerrainType, bool> predicate)
    {
        int count = 0;
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                if (predicate(world.Tiles[x, y].Terrain))
                {
                    count++;
                }
            }
        }
        return count;
    }

    private static float AverageLandVegetation(WorldMap world)
    {
        float total = 0f;
        int count = 0;
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                var tile = world.Tiles[x, y];
                if (tile.Terrain is TerrainType.Sand or TerrainType.Grass or TerrainType.Forest)
                {
                    total += tile.VegetationDensity;
                    count++;
                }
            }
        }
        return count > 0 ? total / count : 0f;
    }
}
