using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Determinism;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.WorldGeneration;

namespace WildSeed.Simulation.Tests.Determinism;

public static class ContractV4GoldenFingerprints
{
    public const int GoldenSeed = 1337;

    private static SimulationState CreateStandardWorld(int seed = GoldenSeed)
    {
        var config = new WorldConfiguration(
            seed: seed,
            width: 64,
            height: 64,
            initialHerbivores: 50,
            initialCarnivores: 10,
            vegetationDensity: 0.5f,
            waterLevel: 0.2f,
            mutationProbability: 0.05f,
            mutationStrength: 0.1f);
        var world = new WorldGenerator().Generate(config);
        return SimulationStateFactory.Create(world);
    }

    public static StateFingerprint ComputeAtTick(int targetTick, int seed = GoldenSeed)
    {
        var engine = new SimulationEngine(CreateStandardWorld(seed));
        if (targetTick > 0) engine.AdvanceTicks(targetTick);
        return SimulationStateFingerprint.Compute(engine.State, SimulationContract.Version4);
    }
}
