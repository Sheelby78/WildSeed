namespace WildSeed.Domain.World;

public sealed record WorldConfiguration
{
    public int Seed { get; }
    public int Width { get; }
    public int Height { get; }
    public int InitialHerbivores { get; }
    public int InitialCarnivores { get; }
    public float VegetationDensity { get; }
    public float WaterLevel { get; }
    public float MutationProbability { get; }
    public float MutationStrength { get; }

    public WorldConfiguration(
        int seed,
        int width,
        int height,
        int initialHerbivores,
        int initialCarnivores,
        float vegetationDensity,
        float waterLevel,
        float mutationProbability,
        float mutationStrength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 64);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, 512);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 64);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(height, 512);

        ArgumentOutOfRangeException.ThrowIfNegative(initialHerbivores);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialHerbivores, 5000);
        ArgumentOutOfRangeException.ThrowIfNegative(initialCarnivores);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCarnivores, 5000);

        ArgumentOutOfRangeException.ThrowIfLessThan(vegetationDensity, 0.0f);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vegetationDensity, 1.0f);

        ArgumentOutOfRangeException.ThrowIfLessThan(waterLevel, 0.0f);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(waterLevel, 1.0f);

        ArgumentOutOfRangeException.ThrowIfLessThan(mutationProbability, 0.0f);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mutationProbability, 1.0f);

        ArgumentOutOfRangeException.ThrowIfLessThan(mutationStrength, 0.0f);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(mutationStrength, 1.0f);

        Seed = seed;
        Width = width;
        Height = height;
        InitialHerbivores = initialHerbivores;
        InitialCarnivores = initialCarnivores;
        VegetationDensity = vegetationDensity;
        WaterLevel = waterLevel;
        MutationProbability = mutationProbability;
        MutationStrength = mutationStrength;
    }
}
