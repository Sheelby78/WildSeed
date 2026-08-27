using WildSeed.Domain.Organisms;
using Xunit;

namespace WildSeed.Domain.Tests;

public sealed class OrganismTests
{
    [Fact]
    public void Constructor_ValidParameters_SetsProperties()
    {
        var id = Guid.NewGuid();
        var genome = new Genome(1.5f);
        var organism = new Organism(id, Species.Herbivore, genome, 12.5f, 45.0f);

        Assert.Equal(id, organism.Id);
        Assert.Equal(Species.Herbivore, organism.Species);
        Assert.Equal(1.5f, organism.Genome.Speed);
        Assert.Equal(12.5f, organism.X);
        Assert.Equal(45.0f, organism.Y);
        Assert.True(organism.IsAlive);
    }

    [Theory]
    [InlineData(-1.0f, 10.0f)]
    [InlineData(10.0f, -1.0f)]
    public void Constructor_NegativeCoordinates_ThrowsArgumentOutOfRangeException(float x, float y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Organism(Guid.NewGuid(), Species.Carnivore, new Genome(1.0f), x, y));
    }

    [Fact]
    public void Genome_ClampsSpeedWithinBounds()
    {
        var low = new Genome(0.01f);
        var high = new Genome(100.0f);
        var normal = new Genome(2.5f);

        Assert.Equal(0.1f, low.Speed);
        Assert.Equal(10.0f, high.Speed);
        Assert.Equal(2.5f, normal.Speed);
    }
}
