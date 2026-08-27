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
    public void Genome_ClampsTraitsWithinBounds()
    {
        var low = new Genome(0.01f, 0.01f, 0.5f);
        var high = new Genome(100.0f, 100.0f, 100.0f);
        var normal = new Genome(2.5f, 1.8f, 12.0f);

        Assert.Equal(0.1f, low.Speed);
        Assert.Equal(0.1f, low.Size);
        Assert.Equal(1.0f, low.Vision);

        Assert.Equal(10.0f, high.Speed);
        Assert.Equal(10.0f, high.Size);
        Assert.Equal(50.0f, high.Vision);

        Assert.Equal(2.5f, normal.Speed);
        Assert.Equal(1.8f, normal.Size);
        Assert.Equal(12.0f, normal.Vision);
    }

    [Fact]
    public void Constructor_WithLineage_SetsLineageProperties()
    {
        var id = Guid.NewGuid();
        var motherId = Guid.NewGuid();
        var fatherId = Guid.NewGuid();
        var organism = new Organism(id, Species.Carnivore, new Genome(1.2f, 1.5f, 10.0f), 5.0f, 8.0f, true, motherId, fatherId, 3);

        Assert.Equal(motherId, organism.MotherId);
        Assert.Equal(fatherId, organism.FatherId);
        Assert.Equal(3, organism.Generation);
    }
}
