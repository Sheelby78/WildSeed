using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;

namespace WildSeed.Simulation.Tests.Behavior;

public sealed class CarnivoreMetabolismTests
{
    [Fact]
    public void Carnivore_DoesNotConsumeVegetation_AndEventuallyStarves()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.5f);

        var config = new WorldConfiguration(42, 64, 64, 0, 1, 0.8f, 0.1f, 0.05f, 0.1f);
        var orgId = Guid.NewGuid();
        var world = new WorldMap(config, tiles, [new Organism(orgId, Species.Carnivore, new Genome(1.0f), 2.0f, 2.0f)]);
        var state = SimulationStateFactory.Create(world);

        state.Organisms[0].Needs = new OrganismNeeds(900, 100, 900);
        var engine = new SimulationEngine(state);
        engine.AdvanceTick();

        Assert.NotEqual(OrganismAction.Eat, state.Organisms[0].Action);
        Assert.NotEqual(OrganismAction.SeekFood, state.Organisms[0].Action);
        Assert.True(state.Organisms[0].Needs.Hunger >= 900);
    }
}
