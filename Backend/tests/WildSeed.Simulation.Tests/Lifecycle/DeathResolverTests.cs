using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;
using WildSeed.Simulation.Lifecycle;
using Xunit;

namespace WildSeed.Simulation.Tests.Lifecycle;

public sealed class DeathResolverTests
{
    private static SimulationState CreateState()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.5f);

        var config = new WorldConfiguration(42, 64, 64, 0, 0, 0.5f, 0.1f, 0.05f, 0.1f);
        var world = new WorldMap(config, tiles, []);
        return SimulationStateFactory.Create(world);
    }

    [Fact]
    public void Resolve_Herbivore_DiesOfOldAge_AtHerbivoreMaxAge()
    {
        var state = CreateState();
        var herbivore = new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 5.0f, 5.0f)
        {
            AgeTicks = SurvivalRulesV4.HerbivoreMaximumAgeTicks,
            Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: 500)
        };
        state.Organisms.Add(herbivore);

        var deaths = DeathResolver.Resolve(state);

        Assert.Single(deaths);
        var death = Assert.IsType<OrganismDied>(deaths[0]);
        Assert.Equal(DeathCause.OldAge, death.Cause);
        Assert.Empty(state.Organisms);
    }

    [Fact]
    public void Resolve_Carnivore_SurvivesPastHerbivoreMaxAge_UntilCarnivoreMaxAge()
    {
        var state = CreateState();
        var carnivore = new OrganismState(Guid.NewGuid(), Species.Carnivore, new Genome(1.0f), 5.0f, 5.0f)
        {
            AgeTicks = SurvivalRulesV4.HerbivoreMaximumAgeTicks + 500,
            Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: 500)
        };
        state.Organisms.Add(carnivore);

        var deaths = DeathResolver.Resolve(state);

        // Carnivore should still be alive because CarnivoreMaxAge = 3500 > 2500
        Assert.Empty(deaths);
        Assert.Single(state.Organisms);

        // Now advance to CarnivoreMaximumAgeTicks
        carnivore.AgeTicks = SurvivalRulesV4.CarnivoreMaximumAgeTicks;
        deaths = DeathResolver.Resolve(state);

        Assert.Single(deaths);
        var death = Assert.IsType<OrganismDied>(deaths[0]);
        Assert.Equal(DeathCause.OldAge, death.Cause);
        Assert.Empty(state.Organisms);
    }
}
