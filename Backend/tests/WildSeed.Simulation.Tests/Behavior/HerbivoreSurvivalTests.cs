using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Behavior;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Perception;
using WildSeed.Simulation.Resources;

namespace WildSeed.Simulation.Tests.Behavior;

public sealed class HerbivoreSurvivalTests
{
    [Fact]
    public void Herbivore_SeeksAndConsumesVegetation_WhenHungry()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.0f);

        var config = new WorldConfiguration(42, 64, 64, 1, 0, 0.0f, 0.1f, 0.05f, 0.1f);
        var orgId = Guid.NewGuid();
        var world = new WorldMap(config, tiles, [new Organism(orgId, Species.Herbivore, new Genome(1.0f), 2.0f, 2.0f)]);
        var state = SimulationStateFactory.Create(world);

        state.Organisms[0].Needs = new OrganismNeeds(500, 100, 900);
        int vegIndex = 2 * 64 + 2;
        state.Vegetation[vegIndex] = new VegetationResource(200, 200);

        var engine = new SimulationEngine(state);
        engine.AdvanceTick();

        Assert.Equal(OrganismAction.Eat, state.Organisms[0].Action);
        Assert.True(state.Organisms[0].Needs.Hunger < 500);
    }

    [Fact]
    public void Organisms_AccumulateThirst_AtReducedCadence()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.0f);

        var config = new WorldConfiguration(42, 64, 64, 1, 0, 0.0f, 0.1f, 0.05f, 0.1f);
        var world = new WorldMap(config, tiles, [new Organism(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f), 2.0f, 2.0f)]);
        var state = SimulationStateFactory.Create(world);
        state.Organisms[0].Needs = new OrganismNeeds(hunger: 0, thirst: 0, energy: 1000);

        var engine = new SimulationEngine(state);

        for (int tick = 1; tick <= SurvivalRulesV2.ThirstMetabolismCadenceTicks * 2; tick++)
        {
            engine.AdvanceTick();
        }

        Assert.Equal(2 * SurvivalRulesV2.MetabolismThirst, state.Organisms[0].Needs.Thirst);
        int expectedHungerTicks = (SurvivalRulesV2.ThirstMetabolismCadenceTicks * 2) / SurvivalRulesV4.HungerMetabolismCadenceTicks;
        Assert.Equal(expectedHungerTicks * SurvivalRulesV4.MetabolismHunger, state.Organisms[0].Needs.Hunger);
    }

    [Fact]
    public void AdvanceTick_WhenHerbivoreEatingIsKilledByPredationInSameTick_DoesNotThrow()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.0f);

        var config = new WorldConfiguration(42, 64, 64, 1, 1, 0.0f, 0.1f, 0.05f, 0.1f);
        var herbId = Guid.NewGuid();
        var carnId = Guid.NewGuid();
        var world = new WorldMap(config, tiles, [
            new Organism(herbId, Species.Herbivore, new Genome(1.0f), 2.0f, 2.0f),
            new Organism(carnId, Species.Carnivore, new Genome(1.0f), 2.0f, 2.0f)
        ]);
        var state = SimulationStateFactory.Create(world);

        state.Organisms[0].Needs = new OrganismNeeds(hunger: 500, thirst: 100, energy: 900);
        state.Organisms[1].Needs = new OrganismNeeds(hunger: 500, thirst: 100, energy: 900);

        int vegIndex = 2 * 64 + 2;
        state.Vegetation[vegIndex] = new VegetationResource(200, 200);

        var engine = new SimulationEngine(state);

        // Advance tick where carnivore attacks herbivore on the same tile herbivore is eating
        var exception = Record.Exception(() => engine.AdvanceTick());
        Assert.Null(exception);
    }
}
