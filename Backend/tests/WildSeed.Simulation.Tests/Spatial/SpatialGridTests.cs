using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Spatial;

namespace WildSeed.Simulation.Tests.Spatial;

public sealed class SpatialGridTests
{
    [Fact]
    public void SpatialGrid_FindsNearestTargetOrganism_WithinRadius()
    {
        var grid = new SpatialGrid(64, 64, cellSize: 8);
        var org1 = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 10.0f, 10.0f);
        var org2 = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 12.0f, 12.0f);
        var org3 = new OrganismState(Guid.NewGuid(), Species.Carnivore, new Genome(1.0f), 50.0f, 50.0f);
        var organisms = new List<OrganismState> { org1, org2, org3 };

        grid.Rebuild(organisms);

        var nearest = grid.FindNearest(10.5f, 10.5f, 5.0f, Species.Herbivore, organisms);
        Assert.NotNull(nearest);
        Assert.Equal(org1.Id, nearest.Id);
    }

    [Fact]
    public void SpatialGrid_ReturnsNull_WhenNoTargetInRadius()
    {
        var grid = new SpatialGrid(64, 64, cellSize: 8);
        var org1 = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 50.0f, 50.0f);
        var organisms = new List<OrganismState> { org1 };

        grid.Rebuild(organisms);

        var nearest = grid.FindNearest(5.0f, 5.0f, 10.0f, Species.Herbivore, organisms);
        Assert.Null(nearest);
    }

    [Fact]
    public void SpatialGrid_FiltersByTargetSpecies()
    {
        var grid = new SpatialGrid(64, 64, cellSize: 8);
        var prey = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 11.0f, 11.0f);
        var predator = new OrganismState(Guid.NewGuid(), Species.Carnivore, new Genome(1.0f), 10.2f, 10.2f);
        var organisms = new List<OrganismState> { prey, predator };

        grid.Rebuild(organisms);

        var nearestPrey = grid.FindNearest(10.0f, 10.0f, 5.0f, Species.Herbivore, organisms);
        Assert.NotNull(nearestPrey);
        Assert.Equal(prey.Id, nearestPrey.Id);

        var nearestPredator = grid.FindNearest(10.0f, 10.0f, 5.0f, Species.Carnivore, organisms);
        Assert.NotNull(nearestPredator);
        Assert.Equal(predator.Id, nearestPredator.Id);
    }
}
