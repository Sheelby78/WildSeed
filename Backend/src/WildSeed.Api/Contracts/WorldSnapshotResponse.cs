using WildSeed.Domain.World;
using WildSeed.Simulation.Determinism;

namespace WildSeed.Api.Contracts;

public sealed record WorldSnapshotResponse(
    int Width,
    int Height,
    TileDto[][] Tiles,
    OrganismDto[] Organisms,
    string Fingerprint)
{
    public static WorldSnapshotResponse FromDomain(WorldMap world, StateFingerprint fingerprint)
    {
        ArgumentNullException.ThrowIfNull(world);

        var tiles = new TileDto[world.Height][];
        for (int y = 0; y < world.Height; y++)
        {
            tiles[y] = new TileDto[world.Width];
            for (int x = 0; x < world.Width; x++)
            {
                var tile = world.Tiles[x, y];
                tiles[y][x] = new TileDto(
                    X: tile.X,
                    Y: tile.Y,
                    Terrain: tile.Terrain.ToString(),
                    VegetationDensity: tile.VegetationDensity);
            }
        }

        var organisms = new OrganismDto[world.Organisms.Count];
        for (int i = 0; i < world.Organisms.Count; i++)
        {
            var org = world.Organisms[i];
            organisms[i] = new OrganismDto(
                Id: org.Id.ToString(),
                Species: org.Species.ToString(),
                X: org.X,
                Y: org.Y,
                Speed: org.Genome.Speed,
                Genome: new GenomeDto(org.Genome.Speed, org.Genome.Size, org.Genome.Vision),
                MotherId: org.MotherId?.ToString(),
                FatherId: org.FatherId?.ToString(),
                Generation: org.Generation);
        }

        return new WorldSnapshotResponse(
            Width: world.Width,
            Height: world.Height,
            Tiles: tiles,
            Organisms: organisms,
            Fingerprint: fingerprint.ToString());
    }
}

public sealed record TileDto(int X, int Y, string Terrain, float VegetationDensity);

public sealed record OrganismDto(
    string Id,
    string Species,
    float X,
    float Y,
    float Speed,
    GenomeDto Genome = default!,
    string? MotherId = null,
    string? FatherId = null,
    int Generation = 1);
