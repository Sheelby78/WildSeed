using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;

namespace WildSeed.Simulation.Tests.Lifecycle;

public sealed class DeathResolutionTests
{
    [Fact]
    public void DehydrationTakesPrecedenceOverStarvation()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.5f);

        var config = new WorldConfiguration(42, 64, 64, 1, 0, 0.5f, 0.1f, 0.05f, 0.1f);
        var orgId = Guid.NewGuid();
        var world = new WorldMap(config, tiles, [new Organism(orgId, Species.Herbivore, new Genome(1.0f), 2.0f, 2.0f)]);
        var state = SimulationStateFactory.Create(world);

        state.Organisms[0].Needs = new OrganismNeeds(1000, 1000, 500);
        var engine = new SimulationEngine(state);
        var result = engine.AdvanceTick();

        Assert.Empty(state.Organisms);
        var died = Assert.Single(result.Events.OfType<OrganismDied>());
        Assert.Equal(DeathCause.Dehydration, died.Cause);
    }
}
