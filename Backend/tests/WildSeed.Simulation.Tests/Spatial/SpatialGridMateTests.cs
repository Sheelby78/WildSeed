using WildSeed.Domain.Organisms;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Spatial;
using Xunit;

namespace WildSeed.Simulation.Tests.Spatial;

public sealed class SpatialGridMateTests
{
    [Fact]
    public void SpatialGrid_FindsNearestEligibleMate_SameSpeciesAndMature()
    {
        var grid = new SpatialGrid(64, 64, cellSize: 8);
        var selfId = Guid.NewGuid();
        var self = new OrganismState(selfId, Species.Herbivore, new Genome(1.0f), 10.0f, 10.0f);

        var eligibleMate = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 12.0f, 10.0f)
        {
            AgeTicks = SurvivalRulesV4.MaturationAgeTicks,
            Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: SurvivalRulesV4.MatingEnergyThreshold + 50),
            ReproductionCooldownTicks = 0
        };

        var tooYoung = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 11.0f, 10.0f)
        {
            AgeTicks = 20,
            Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: 800),
            ReproductionCooldownTicks = 0
        };

        var inCooldown = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 10.5f, 10.0f)
        {
            AgeTicks = 200,
            Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: 800),
            ReproductionCooldownTicks = 50
        };

        var carnivore = new OrganismState(Guid.NewGuid(), Species.Carnivore, new Genome(1.0f), 10.2f, 10.0f)
        {
            AgeTicks = 200,
            Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: 800),
            ReproductionCooldownTicks = 0
        };

        var organisms = new List<OrganismState> { self, eligibleMate, tooYoung, inCooldown, carnivore };
        grid.Rebuild(organisms);

        var mate = grid.FindNearestEligibleMate(self.X, self.Y, 10.0f, Species.Herbivore, selfId, organisms);
        Assert.NotNull(mate);
        Assert.Equal(eligibleMate.Id, mate.Id);
    }

    [Fact]
    public void SpatialGrid_ReturnsNull_WhenNoEligibleMateInRadius()
    {
        var grid = new SpatialGrid(64, 64, cellSize: 8);
        var selfId = Guid.NewGuid();
        var self = new OrganismState(selfId, Species.Carnivore, new Genome(1.0f), 10.0f, 10.0f);

        var hungryMate = new OrganismState(Guid.NewGuid(), Species.Carnivore, new Genome(1.0f), 12.0f, 10.0f)
        {
            AgeTicks = 200,
            Needs = new OrganismNeeds(hunger: SurvivalRulesV4.CriticalNeed + 10, thirst: 100, energy: 800),
            ReproductionCooldownTicks = 0
        };

        var organisms = new List<OrganismState> { self, hungryMate };
        grid.Rebuild(organisms);

        var mate = grid.FindNearestEligibleMate(self.X, self.Y, 10.0f, Species.Carnivore, selfId, organisms);
        Assert.Null(mate);
    }
}
