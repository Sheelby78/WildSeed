using WildSeed.Domain.Terrain;
using Xunit;

namespace WildSeed.Domain.Tests;

public sealed class TileTests
{
    [Fact]
    public void Constructor_LandTile_ClampsVegetationDensity()
    {
        var tileLow = new Tile(5, 10, TerrainType.Grass, -0.5f);
        var tileHigh = new Tile(5, 10, TerrainType.Forest, 1.5f);
        var tileNormal = new Tile(5, 10, TerrainType.Sand, 0.4f);

        Assert.Equal(0.0f, tileLow.VegetationDensity);
        Assert.Equal(1.0f, tileHigh.VegetationDensity);
        Assert.Equal(0.4f, tileNormal.VegetationDensity);
    }

    [Theory]
    [InlineData(TerrainType.DeepWater)]
    [InlineData(TerrainType.ShallowWater)]
    public void Constructor_WaterTile_ForcesZeroVegetationDensity(TerrainType waterTerrain)
    {
        var tile = new Tile(2, 3, waterTerrain, 0.8f);

        Assert.Equal(0.0f, tile.VegetationDensity);
    }
}
