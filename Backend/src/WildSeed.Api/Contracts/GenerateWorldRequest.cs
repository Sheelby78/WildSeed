using WildSeed.Domain.World;

namespace WildSeed.Api.Contracts;

public sealed record GenerateWorldRequest
{
    public int? Seed { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? InitialHerbivores { get; init; }
    public int? InitialCarnivores { get; init; }
    public float? VegetationDensity { get; init; }
    public float? WaterLevel { get; init; }
    public float? MutationProbability { get; init; }
    public float? MutationStrength { get; init; }

    public WorldConfiguration ToDomain()
    {
        return new WorldConfiguration(
            seed: Seed ?? 42,
            width: Width ?? 128,
            height: Height ?? 128,
            initialHerbivores: InitialHerbivores ?? 50,
            initialCarnivores: InitialCarnivores ?? 10,
            vegetationDensity: VegetationDensity ?? 0.5f,
            waterLevel: WaterLevel ?? 0.45f,
            mutationProbability: MutationProbability ?? 0.05f,
            mutationStrength: MutationStrength ?? 0.1f);
    }
}
