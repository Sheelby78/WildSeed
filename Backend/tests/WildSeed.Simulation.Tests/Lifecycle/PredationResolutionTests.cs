using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;

namespace WildSeed.Simulation.Tests.Lifecycle;

public sealed class PredationResolutionTests
{
    [Fact]
    public void Carnivore_AttacksAndKillsPrey_SatisfyingHunger()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.0f);

        var config = new WorldConfiguration(42, 64, 64, 1, 1, 0.0f, 0.1f, 0.05f, 0.1f);
        var carnId = Guid.NewGuid();
        var preyId = Guid.NewGuid();

        var world = new WorldMap(config, tiles,
        [
            new Organism(carnId, Species.Carnivore, new Genome(1.0f), 10.0f, 10.0f),
            new Organism(preyId, Species.Herbivore, new Genome(1.0f), 10.5f, 10.5f)
        ]);

        var state = SimulationStateFactory.Create(world);
        state.Organisms.First(o => o.Id == carnId).Needs = new OrganismNeeds(hunger: 500, thirst: 0, energy: 800);
        state.Organisms.First(o => o.Id == preyId).Needs = new OrganismNeeds(hunger: 100, thirst: 100, energy: 800);

        var engine = new SimulationEngine(state);
        var result = engine.AdvanceTick();

        Assert.Single(state.Organisms);
        Assert.Equal(carnId, state.Organisms[0].Id);
        Assert.True(state.Organisms[0].Needs.Hunger < 500);

        var deathEvent = Assert.Single(result.Events.OfType<OrganismDied>());
        Assert.Equal(preyId, deathEvent.OrganismId);
        Assert.Equal(DeathCause.Predation, deathEvent.Cause);
    }

    [Fact]
    public void Carnivore_PursuesPrey_AtSprintSpeed()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.0f);

        var config = new WorldConfiguration(42, 64, 64, 1, 1, 0.0f, 0.1f, 0.05f, 0.1f);
        var carnId = Guid.NewGuid();
        var preyId = Guid.NewGuid();

        var world = new WorldMap(config, tiles,
        [
            new Organism(carnId, Species.Carnivore, new Genome(10.0f), 10.0f, 10.0f),
            new Organism(preyId, Species.Herbivore, new Genome(1.0f), 15.0f, 10.0f)
        ]);

        var state = SimulationStateFactory.Create(world);
        var carn = state.Organisms.First(o => o.Id == carnId);
        carn.Needs = new OrganismNeeds(hunger: 400, thirst: 0, energy: 1000);

        var engine = new SimulationEngine(state);
        engine.AdvanceTick();

        Assert.Equal(OrganismAction.Hunt, carn.Action);
        Assert.True(carn.X > 10.0f);
    }
}
