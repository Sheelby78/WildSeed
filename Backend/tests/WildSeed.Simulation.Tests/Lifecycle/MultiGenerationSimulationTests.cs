using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;
using Xunit;

namespace WildSeed.Simulation.Tests.Lifecycle;

public sealed class MultiGenerationSimulationTests
{
    [Fact]
    public void SimulationEngine_AdvancesTicks_ProducesBirthsAndEvolvesGenerations()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            tiles[x, y] = new Tile(x, y, TerrainType.Grass, 1.0f);

        var config = new WorldConfiguration(
            seed: 42,
            width: 64,
            height: 64,
            initialHerbivores: 10,
            initialCarnivores: 0,
            vegetationDensity: 0.8f,
            waterLevel: 0.1f,
            mutationProbability: 0.1f,
            mutationStrength: 0.2f);

        var parent1 = new Organism(Guid.NewGuid(), Species.Herbivore, new Genome(1.2f, 1.0f, 8.0f), 10.0f, 10.0f, generation: 1);
        var parent2 = new Organism(Guid.NewGuid(), Species.Herbivore, new Genome(1.4f, 1.2f, 9.0f), 10.5f, 10.5f, generation: 1);

        var world = new WorldMap(config, tiles, [parent1, parent2]);
        var state = SimulationStateFactory.Create(world);

        foreach (var org in state.Organisms)
        {
            org.AgeTicks = SurvivalRulesV4.MaturationAgeTicks + 10;
            org.Needs = new OrganismNeeds(hunger: 50, thirst: 50, energy: 900);
        }

        var engine = new SimulationEngine(state);
        var result = engine.AdvanceTick();

        var bornEvents = result.Events.OfType<OrganismBorn>().ToList();
        Assert.NotEmpty(bornEvents);
        Assert.Equal(3, engine.State.Organisms.Count);

        var newborn = engine.State.Organisms.First(o => o.Generation == 2);
        Assert.Equal(2, newborn.Generation);
        Assert.NotNull(newborn.MotherId);
        Assert.NotNull(newborn.FatherId);
    }

    [Fact]
    public void Metabolism_ScalesWithOrganismSizeAndSpeed()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.5f);

        var config = new WorldConfiguration(42, 64, 64, 2, 0, 0.5f, 0.1f, 0.05f, 0.1f);
        var small = new Organism(Guid.NewGuid(), Species.Herbivore, new Genome(speed: 1.0f, size: 0.5f), 10.0f, 10.0f);
        var large = new Organism(Guid.NewGuid(), Species.Herbivore, new Genome(speed: 2.0f, size: 2.0f), 20.0f, 20.0f);

        var world = new WorldMap(config, tiles, [small, large]);
        var state = SimulationStateFactory.Create(world);

        var engine = new SimulationEngine(state);
        engine.AdvanceTick();

        var smallOrg = state.Organisms.First(o => o.Id == small.Id);
        var largeOrg = state.Organisms.First(o => o.Id == large.Id);

        // Larger size increases hunger cost: ceil(1 * 0.5) = 1 vs ceil(1 * 2.0) = 2
        Assert.True(largeOrg.Needs.Hunger >= smallOrg.Needs.Hunger);
    }

    [Fact]
    public void SimulationEngine_PopulationGrowsNaturally_WhenAbundantVegetation()
    {
        var config = new WorldConfiguration(
            seed: 1234,
            width: 64,
            height: 64,
            initialHerbivores: 50,
            initialCarnivores: 0,
            vegetationDensity: 0.8f,
            waterLevel: 0.1f,
            mutationProbability: 0.05f,
            mutationStrength: 0.1f);

        var world = new WildSeed.Simulation.WorldGeneration.WorldGenerator().Generate(config);
        var state = SimulationStateFactory.Create(world);
        var engine = new SimulationEngine(state);

        int totalBirths = 0;
        for (int i = 0; i < 400; i++)
        {
            var res = engine.AdvanceTick();
            totalBirths += res.Events.OfType<OrganismBorn>().Count();
        }

        Assert.True(totalBirths > 0, $"Expected births after 400 ticks, but got {totalBirths}");
    }
}
