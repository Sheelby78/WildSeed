using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;

namespace WildSeed.Domain.World;

public sealed class WorldMap
{
    public WorldConfiguration Configuration { get; }
    public int Width => Configuration.Width;
    public int Height => Configuration.Height;
    public Tile[,] Tiles { get; }
    public IReadOnlyList<Organism> Organisms { get; }

    public WorldMap(WorldConfiguration configuration, Tile[,] tiles, IReadOnlyList<Organism> organisms)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(organisms);

        if (tiles.GetLength(0) != configuration.Width || tiles.GetLength(1) != configuration.Height)
        {
            throw new ArgumentException("Tile array dimensions must match configuration width and height.", nameof(tiles));
        }

        Configuration = configuration;
        Tiles = tiles;
        Organisms = organisms;
    }

    public Tile GetTile(int x, int y)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);

        return Tiles[x, y];
    }
}
