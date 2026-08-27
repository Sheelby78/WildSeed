namespace WildSeed.Domain.Terrain;

public readonly record struct Tile
{
    public int X { get; }
    public int Y { get; }
    public TerrainType Terrain { get; }
    public float VegetationDensity { get; }

    public Tile(int x, int y, TerrainType terrain, float vegetationDensity)
    {
        X = x;
        Y = y;
        Terrain = terrain;
        VegetationDensity = terrain is TerrainType.DeepWater or TerrainType.ShallowWater
            ? 0f
            : Math.Clamp(vegetationDensity, 0f, 1f);
    }
}
