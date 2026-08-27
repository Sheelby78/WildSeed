using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Determinism;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;
using WildSeed.Simulation.WorldGeneration;
using Xunit;

namespace WildSeed.Simulation.Tests.Determinism;

public sealed class InheritedEvolutionDeterminismTests
{
    private static SimulationState CreateStandardWorld(int seed = 1337)
    {
        var config = new WorldConfiguration(
            seed: seed,
            width: 64,
            height: 64,
            initialHerbivores: 50,
            initialCarnivores: 10,
            vegetationDensity: 0.6f,
            waterLevel: 0.2f,
            mutationProbability: 0.1f,
            mutationStrength: 0.2f);
        var world = new WorldGenerator().Generate(config);
        return SimulationStateFactory.Create(world);
    }

    [Fact]
    public void TwoIndependentRuns_ProduceIdenticalStateAndLineages()
    {
        var engineA = new SimulationEngine(CreateStandardWorld());
        var engineB = new SimulationEngine(CreateStandardWorld());

        var birthsA = new List<OrganismBorn>();
        var birthsB = new List<OrganismBorn>();

        for (int i = 0; i < 50; i++)
        {
            var resA = engineA.AdvanceTick();
            var resB = engineB.AdvanceTick();

            birthsA.AddRange(resA.Events.OfType<OrganismBorn>());
            birthsB.AddRange(resB.Events.OfType<OrganismBorn>());
        }

        var fpA = SimulationStateFingerprint.Compute(engineA.State, SimulationContract.Version4);
        var fpB = SimulationStateFingerprint.Compute(engineB.State, SimulationContract.Version4);

        Assert.Equal(fpA, fpB);
        Assert.Equal(birthsA.Count, birthsB.Count);

        for (int i = 0; i < birthsA.Count; i++)
        {
            Assert.Equal(birthsA[i].OrganismId, birthsB[i].OrganismId);
            Assert.Equal(birthsA[i].Species, birthsB[i].Species);
            Assert.Equal(birthsA[i].Generation, birthsB[i].Generation);
            Assert.Equal(birthsA[i].MotherId, birthsB[i].MotherId);
            Assert.Equal(birthsA[i].FatherId, birthsB[i].FatherId);
            Assert.Equal(birthsA[i].Genome.Speed, birthsB[i].Genome.Speed);
            Assert.Equal(birthsA[i].Genome.Size, birthsB[i].Genome.Size);
            Assert.Equal(birthsA[i].Genome.Vision, birthsB[i].Genome.Vision);
            Assert.Equal(birthsA[i].Tick, birthsB[i].Tick);
        }
    }

    [Fact]
    public void SingleTickAndBatchAdvance_ProduceIdenticalV4State()
    {
        var engineSingle = new SimulationEngine(CreateStandardWorld());
        var engineBatch = new SimulationEngine(CreateStandardWorld());

        for (int i = 0; i < 30; i++) engineSingle.AdvanceTick();
        engineBatch.AdvanceTicks(30);

        var fpSingle = SimulationStateFingerprint.Compute(engineSingle.State, SimulationContract.Version4);
        var fpBatch = SimulationStateFingerprint.Compute(engineBatch.State, SimulationContract.Version4);

        Assert.Equal(fpSingle, fpBatch);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void V4GoldenCheckpoints_AreDeterministicAndStable(int tick)
    {
        var fp1 = ContractV4GoldenFingerprints.ComputeAtTick(tick);
        var fp2 = ContractV4GoldenFingerprints.ComputeAtTick(tick);
        Assert.Equal(fp1, fp2);
        Assert.StartsWith("v4:", fp1.ToString());
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentGenerationalLineages()
    {
        var engine1 = new SimulationEngine(CreateStandardWorld(1001));
        var engine2 = new SimulationEngine(CreateStandardWorld(2002));

        engine1.AdvanceTicks(50);
        engine2.AdvanceTicks(50);

        var fp1 = SimulationStateFingerprint.Compute(engine1.State, SimulationContract.Version4);
        var fp2 = SimulationStateFingerprint.Compute(engine2.State, SimulationContract.Version4);

        Assert.NotEqual(fp1, fp2);
    }
}
