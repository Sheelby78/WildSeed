using WildSeed.Domain.World;
using WildSeed.Simulation.Determinism;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.WorldGeneration;

namespace WildSeed.Simulation.Tests.Determinism;

public sealed class SurvivalLoopDeterminismTests
{
    private static SimulationState CreateStandardWorld()
    {
        var config = new WorldConfiguration(
            seed: 1337,
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

    [Fact]
    public void TwoIndependentRuns_ProduceIdenticalFingerprints()
    {
        var engineA = new SimulationEngine(CreateStandardWorld());
        var engineB = new SimulationEngine(CreateStandardWorld());

        for (int i = 0; i < 50; i++)
        {
            engineA.AdvanceTick();
            engineB.AdvanceTick();
        }

        var fpA = SimulationStateFingerprint.Compute(engineA.State);
        var fpB = SimulationStateFingerprint.Compute(engineB.State);

        Assert.Equal(fpA, fpB);
    }

    [Fact]
    public void SingleTickAndBatchAdvance_ProduceIdenticalState()
    {
        var engineSingle = new SimulationEngine(CreateStandardWorld());
        var engineBatch = new SimulationEngine(CreateStandardWorld());

        for (int i = 0; i < 20; i++) engineSingle.AdvanceTick();
        engineBatch.AdvanceTicks(20);

        var fpSingle = SimulationStateFingerprint.Compute(engineSingle.State);
        var fpBatch = SimulationStateFingerprint.Compute(engineBatch.State);

        Assert.Equal(fpSingle, fpBatch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    public void GoldenCheckpoints_AreDeterministicAndStable(int tick)
    {
        var fp1 = ContractV2GoldenFingerprints.ComputeAtTick(tick);
        var fp2 = ContractV2GoldenFingerprints.ComputeAtTick(tick);
        Assert.Equal(fp1, fp2);
        Assert.StartsWith("v2:", fp1.ToString());
    }
}
