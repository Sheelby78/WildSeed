using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Movement;
using WildSeed.Simulation.WorldGeneration;
using Xunit;

namespace WildSeed.Simulation.Tests.Movement;

public sealed class MovementResolverTests
{
    [Fact]
    public void ExploreMovement_OverConsecutiveTicks_DisplacesOrganism()
    {
        var config = new WorldConfiguration(
            seed: 42,
            width: 64,
            height: 64,
            initialHerbivores: 1,
            initialCarnivores: 0,
            vegetationDensity: 0.0f,
            waterLevel: 0.05f,
            mutationProbability: 0.05f,
            mutationStrength: 0.1f);

        var world = new WorldGenerator().Generate(config);
        var state = SimulationStateFactory.Create(world);
        var organism = state.Organisms[0];
        var engine = new SimulationEngine(state);

        float startX = organism.X;
        float startY = organism.Y;

        for (int i = 0; i < 30; i++)
        {
            engine.AdvanceTick();
        }

        float distanceTraveled = MathF.Sqrt(MathF.Pow(organism.X - startX, 2) + MathF.Pow(organism.Y - startY, 2));
        Assert.True(distanceTraveled > 0.5f, $"Distance: {distanceTraveled}, Start: ({startX}, {startY}), End: ({organism.X}, {organism.Y}), Action: {organism.Action}, Energy: {organism.Needs.Energy}, Hunger: {organism.Needs.Hunger}");
    }

    [Fact]
    public void TargetMovement_WhenDirectPathBlockedByWater_NavigatesAroundWater()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.5f);

        // Put a small water block between organism (10, 10) and target (15, 10) at (12, 10)
        tiles[12, 10] = new Tile(12, 10, TerrainType.ShallowWater, 0.0f);

        var config = new WorldConfiguration(42, 64, 64, 1, 0, 0.5f, 0.1f, 0.05f, 0.1f);
        var world = new WorldMap(config, tiles, [new Organism(Guid.NewGuid(), Species.Carnivore, new Genome(2.0f), 10.5f, 10.5f)]);
        var state = SimulationStateFactory.Create(world);
        var organism = state.Organisms[0];

        var resolver = new MovementResolver();
        float initialX = organism.X;

        // Try to move toward target behind water
        resolver.Move(state, organism, (15, 10), isFleeing: false, speedMultiplier: 1.0f);

        // The organism should not freeze; it should move along the angled deviation
        Assert.True(organism.X != initialX || organism.Y != 10.5f);
        Assert.NotEqual(TerrainType.ShallowWater, state.World.Tiles[(int)organism.X, (int)organism.Y].Terrain);
    }
}
