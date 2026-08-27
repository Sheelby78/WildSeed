using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Determinism;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;
using WildSeed.Simulation.WorldGeneration;

namespace WildSeed.Simulation.Tests.Determinism;

public sealed class PredatorPreyDeterminismTests
{
    private static SimulationState CreateStandardWorld(int seed = 1337)
    {
        var config = new WorldConfiguration(
            seed: seed,
            width: 64,
            height: 64,
            initialHerbivores: 50,
            initialCarnivores: 15,
            vegetationDensity: 0.5f,
            waterLevel: 0.2f,
            mutationProbability: 0.05f,
            mutationStrength: 0.1f);
        var world = new WorldGenerator().Generate(config);
        return SimulationStateFactory.Create(world);
    }

    [Fact]
    public void TwoIndependentRuns_ProduceIdenticalStateAndPredationEvents()
    {
        var engineA = new SimulationEngine(CreateStandardWorld());
        var engineB = new SimulationEngine(CreateStandardWorld());

        var predationDeathsA = new List<OrganismDied>();
        var predationDeathsB = new List<OrganismDied>();

        for (int i = 0; i < 50; i++)
        {
            var resA = engineA.AdvanceTick();
            var resB = engineB.AdvanceTick();

            predationDeathsA.AddRange(resA.Events.OfType<OrganismDied>());
            predationDeathsB.AddRange(resB.Events.OfType<OrganismDied>());
        }

        var fpA = SimulationStateFingerprint.Compute(engineA.State, SimulationContract.Version3);
        var fpB = SimulationStateFingerprint.Compute(engineB.State, SimulationContract.Version3);

        Assert.Equal(fpA, fpB);
        Assert.Equal(predationDeathsA.Count, predationDeathsB.Count);

        for (int i = 0; i < predationDeathsA.Count; i++)
        {
            Assert.Equal(predationDeathsA[i].OrganismId, predationDeathsB[i].OrganismId);
            Assert.Equal(predationDeathsA[i].Cause, predationDeathsB[i].Cause);
            Assert.Equal(predationDeathsA[i].Tick, predationDeathsB[i].Tick);
        }
    }

    [Fact]
    public void SingleTickAndBatchAdvance_ProduceIdenticalV3State()
    {
        var engineSingle = new SimulationEngine(CreateStandardWorld());
        var engineBatch = new SimulationEngine(CreateStandardWorld());

        for (int i = 0; i < 30; i++) engineSingle.AdvanceTick();
        engineBatch.AdvanceTicks(30);

        var fpSingle = SimulationStateFingerprint.Compute(engineSingle.State, SimulationContract.Version3);
        var fpBatch = SimulationStateFingerprint.Compute(engineBatch.State, SimulationContract.Version3);

        Assert.Equal(fpSingle, fpBatch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void V3GoldenCheckpoints_AreDeterministicAndStable(int tick)
    {
        var fp1 = ContractV3GoldenFingerprints.ComputeAtTick(tick);
        var fp2 = ContractV3GoldenFingerprints.ComputeAtTick(tick);
        Assert.Equal(fp1, fp2);
        Assert.StartsWith("v3:", fp1.ToString());
    }
}
