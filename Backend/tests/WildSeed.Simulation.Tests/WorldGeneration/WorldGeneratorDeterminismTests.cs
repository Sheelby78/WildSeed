using WildSeed.Domain.World;
using WildSeed.Simulation.WorldGeneration;
using Xunit;

namespace WildSeed.Simulation.Tests.WorldGeneration;

public sealed class WorldGeneratorDeterminismTests
{
    [Fact]
    public void SameConfiguration_ProducesIdenticalWorldFingerprint_AcrossIndependentRuns()
    {
        var config = new WorldConfiguration(
            seed: 1337,
            width: 128,
            height: 128,
            initialHerbivores: 100,
            initialCarnivores: 25,
            vegetationDensity: 0.75f,
            waterLevel: 0.45f,
            mutationProbability: 0.05f,
            mutationStrength: 0.15f);

        var generatorA = new WorldGenerator();
        var generatorB = new WorldGenerator();

        var worldA = generatorA.Generate(config);
        var worldB = generatorB.Generate(config);

        var fingerprintA = WorldFingerprint.Compute(worldA);
        var fingerprintB = WorldFingerprint.Compute(worldB);

        Assert.Equal(fingerprintA, fingerprintB);
        Assert.Equal(fingerprintA.Digest, fingerprintB.Digest);
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentFingerprint()
    {
        var generator = new WorldGenerator();

        var config1 = new WorldConfiguration(1001, 128, 128, 50, 10, 0.5f, 0.5f, 0.05f, 0.1f);
        var config2 = new WorldConfiguration(2002, 128, 128, 50, 10, 0.5f, 0.5f, 0.05f, 0.1f);

        var world1 = generator.Generate(config1);
        var world2 = generator.Generate(config2);

        var fp1 = WorldFingerprint.Compute(world1);
        var fp2 = WorldFingerprint.Compute(world2);

        Assert.NotEqual(fp1, fp2);
    }

    [Theory]
    [InlineData(0.2f, 0.8f)]
    public void ChangingWaterLevel_ProducesDifferentFingerprint(float water1, float water2)
    {
        var generator = new WorldGenerator();

        var config1 = new WorldConfiguration(42, 128, 128, 50, 10, 0.5f, water1, 0.05f, 0.1f);
        var config2 = new WorldConfiguration(42, 128, 128, 50, 10, 0.5f, water2, 0.05f, 0.1f);

        var world1 = generator.Generate(config1);
        var world2 = generator.Generate(config2);

        var fp1 = WorldFingerprint.Compute(world1);
        var fp2 = WorldFingerprint.Compute(world2);

        Assert.NotEqual(fp1, fp2);
    }
}
