using WildSeed.Domain.World;
using Xunit;

namespace WildSeed.Domain.Tests;

public sealed class WorldConfigurationTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var config = new WorldConfiguration(
            seed: 42,
            width: 128,
            height: 128,
            initialHerbivores: 50,
            initialCarnivores: 10,
            vegetationDensity: 0.7f,
            waterLevel: 0.4f,
            mutationProbability: 0.05f,
            mutationStrength: 0.1f);

        Assert.Equal(42, config.Seed);
        Assert.Equal(128, config.Width);
        Assert.Equal(128, config.Height);
        Assert.Equal(50, config.InitialHerbivores);
        Assert.Equal(10, config.InitialCarnivores);
        Assert.Equal(0.7f, config.VegetationDensity);
        Assert.Equal(0.4f, config.WaterLevel);
        Assert.Equal(0.05f, config.MutationProbability);
        Assert.Equal(0.1f, config.MutationStrength);
    }

    [Theory]
    [InlineData(63)]
    [InlineData(513)]
    public void Constructor_InvalidWidth_ThrowsArgumentOutOfRangeException(int width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorldConfiguration(42, width, 128, 50, 10, 0.5f, 0.5f, 0.05f, 0.1f));
    }

    [Theory]
    [InlineData(63)]
    [InlineData(513)]
    public void Constructor_InvalidHeight_ThrowsArgumentOutOfRangeException(int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorldConfiguration(42, 128, height, 50, 10, 0.5f, 0.5f, 0.05f, 0.1f));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5001)]
    public void Constructor_InvalidHerbivoreCount_ThrowsArgumentOutOfRangeException(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorldConfiguration(42, 128, 128, count, 10, 0.5f, 0.5f, 0.05f, 0.1f));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5001)]
    public void Constructor_InvalidCarnivoreCount_ThrowsArgumentOutOfRangeException(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorldConfiguration(42, 128, 128, 50, count, 0.5f, 0.5f, 0.05f, 0.1f));
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Constructor_InvalidVegetationDensity_ThrowsArgumentOutOfRangeException(float density)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorldConfiguration(42, 128, 128, 50, 10, density, 0.5f, 0.05f, 0.1f));
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Constructor_InvalidWaterLevel_ThrowsArgumentOutOfRangeException(float waterLevel)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorldConfiguration(42, 128, 128, 50, 10, 0.5f, waterLevel, 0.05f, 0.1f));
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Constructor_InvalidMutationProbability_ThrowsArgumentOutOfRangeException(float prob)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorldConfiguration(42, 128, 128, 50, 10, 0.5f, 0.5f, prob, 0.1f));
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Constructor_InvalidMutationStrength_ThrowsArgumentOutOfRangeException(float strength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorldConfiguration(42, 128, 128, 50, 10, 0.5f, 0.5f, 0.05f, strength));
    }
}
