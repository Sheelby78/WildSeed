using WildSeed.Domain.Organisms;
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
        Assert.True(distanceTraveled > 0.5f);
    }
}
