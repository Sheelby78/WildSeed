using System.Buffers.Binary;
using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Noise;
using WildSeed.Simulation.Random;

namespace WildSeed.Simulation.WorldGeneration;

public sealed class WorldGenerator
{
    public WorldMap Generate(WorldConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var elevationNoise = new FastNoiseLite(config.Seed);
        elevationNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        elevationNoise.SetFrequency(0.015f);
        elevationNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        elevationNoise.SetFractalOctaves(4);

        var vegNoise = new FastNoiseLite(config.Seed ^ 0x5A5A5A5A);
        vegNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        vegNoise.SetFrequency(0.03f);

        var tiles = new Tile[config.Width, config.Height];
        var landCoordinates = new List<(int X, int Y)>();

        float waterCutoff = Math.Clamp(config.WaterLevel, 0.05f, 0.95f);

        for (int y = 0; y < config.Height; y++)
        {
            for (int x = 0; x < config.Width; x++)
            {
                float rawElevation = elevationNoise.GetNoise(x, y);
                float elevation = (rawElevation + 1.0f) * 0.5f;

                TerrainType terrain;
                float vegRaw = (vegNoise.GetNoise(x, y) + 1.0f) * 0.5f;
                float vegetation;

                if (elevation < waterCutoff * 0.6f)
                {
                    terrain = TerrainType.DeepWater;
                    vegetation = 0.0f;
                }
                else if (elevation < waterCutoff)
                {
                    terrain = TerrainType.ShallowWater;
                    vegetation = 0.0f;
                }
                else if (elevation < waterCutoff + 0.08f)
                {
                    terrain = TerrainType.Sand;
                    vegetation = Math.Clamp(vegRaw * 0.2f * config.VegetationDensity, 0.0f, 1.0f);
                    landCoordinates.Add((x, y));
                }
                else if (elevation < waterCutoff + 0.35f)
                {
                    terrain = TerrainType.Grass;
                    vegetation = Math.Clamp((0.2f + 0.6f * vegRaw) * config.VegetationDensity, 0.0f, 1.0f);
                    landCoordinates.Add((x, y));
                }
                else
                {
                    terrain = TerrainType.Forest;
                    vegetation = Math.Clamp((0.5f + 0.5f * vegRaw) * config.VegetationDensity, 0.0f, 1.0f);
                    landCoordinates.Add((x, y));
                }

                tiles[x, y] = new Tile(x, y, terrain, vegetation);
            }
        }

        var rng = new SimulationRandom(config.Seed + 1);
        var organisms = new List<Organism>(config.InitialHerbivores + config.InitialCarnivores);

        int organismIndex = 0;

        for (int i = 0; i < config.InitialHerbivores; i++)
        {
            (float spawnX, float spawnY) = PickSpawnLocation(landCoordinates, config.Width, config.Height, rng);
            Guid id = CreateDeterministicId(config.Seed, ++organismIndex);
            organisms.Add(new Organism(id, Species.Herbivore, new Genome(1.0f), spawnX, spawnY));
        }

        for (int i = 0; i < config.InitialCarnivores; i++)
        {
            (float spawnX, float spawnY) = PickSpawnLocation(landCoordinates, config.Width, config.Height, rng);
            Guid id = CreateDeterministicId(config.Seed, ++organismIndex);
            organisms.Add(new Organism(id, Species.Carnivore, new Genome(1.2f), spawnX, spawnY));
        }

        return new WorldMap(config, tiles, organisms);
    }

    private static (float X, float Y) PickSpawnLocation(
        List<(int X, int Y)> landCoordinates,
        int width,
        int height,
        SimulationRandom rng)
    {
        if (landCoordinates.Count > 0)
        {
            int index = rng.NextInt(0, landCoordinates.Count);
            var (tileX, tileY) = landCoordinates[index];
            float offsetX = rng.NextFloat(0.1f, 0.9f);
            float offsetY = rng.NextFloat(0.1f, 0.9f);
            return (tileX + offsetX, tileY + offsetY);
        }

        return (rng.NextFloat(0.0f, width), rng.NextFloat(0.0f, height));
    }

    private static Guid CreateDeterministicId(int seed, int index)
    {
        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(bytes[..4], seed);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(4, 4), index);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.Slice(8, 8), 0x57494C4453454544L);
        return new Guid(bytes);
    }
}
