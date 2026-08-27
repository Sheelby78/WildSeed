using WildSeed.Domain.World;
using WildSeed.Simulation.Contracts;
using WildSeed.Simulation.Determinism;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.WorldGeneration;
using Xunit;

namespace WildSeed.Simulation.Tests.Determinism;

public sealed class StatisticsDeterminismTests
{
    private static SimulationState CreateStandardWorld(int seed = 42)
    {
        var config = new WorldConfiguration(
            seed: seed,
            width: 64,
            height: 64,
            initialHerbivores: 40,
            initialCarnivores: 10,
            vegetationDensity: 0.6f,
            waterLevel: 0.2f,
            mutationProbability: 0.1f,
            mutationStrength: 0.15f);
        var world = new WorldGenerator().Generate(config);
        return SimulationStateFactory.Create(world);
    }

    [Fact]
    public void TwoEngines_WithIdenticalSeed_ProduceBitForBitIdenticalStatisticsAndFingerprints()
    {
        var engineA = new SimulationEngine(CreateStandardWorld());
        var engineB = new SimulationEngine(CreateStandardWorld());

        for (int i = 0; i < 100; i++)
        {
            engineA.AdvanceTick();
            engineB.AdvanceTick();
        }

        var fpA = SimulationStateFingerprint.Compute(engineA.State, SimulationContract.Version4);
        var fpB = SimulationStateFingerprint.Compute(engineB.State, SimulationContract.Version4);

        Assert.Equal(fpA, fpB);

        var summaryA = engineA.Statistics.GetSummary(engineA.State);
        var summaryB = engineB.Statistics.GetSummary(engineB.State);

        Assert.Equal(summaryA.TotalPopulation, summaryB.TotalPopulation);
        Assert.Equal(summaryA.Herbivores, summaryB.Herbivores);
        Assert.Equal(summaryA.Carnivores, summaryB.Carnivores);
        Assert.Equal(summaryA.TotalBirths, summaryB.TotalBirths);
        Assert.Equal(summaryA.TotalDeaths, summaryB.TotalDeaths);
        Assert.Equal(summaryA.OverallTraits.AverageSpeed, summaryB.OverallTraits.AverageSpeed);
        Assert.Equal(summaryA.OverallTraits.AverageSize, summaryB.OverallTraits.AverageSize);
        Assert.Equal(summaryA.OverallTraits.AverageVision, summaryB.OverallTraits.AverageVision);
        Assert.Equal(summaryA.Mortality.TotalDeaths, summaryB.Mortality.TotalDeaths);
        Assert.Equal(summaryA.Mortality.AverageLifespanTicks, summaryB.Mortality.AverageLifespanTicks);

        Assert.Equal(engineA.Statistics.History.Count, engineB.Statistics.History.Count);
        for (int i = 0; i < engineA.Statistics.History.Count; i++)
        {
            var pA = engineA.Statistics.History[i];
            var pB = engineB.Statistics.History[i];

            Assert.Equal(pA.Tick, pB.Tick);
            Assert.Equal(pA.TotalPopulation, pB.TotalPopulation);
            Assert.Equal(pA.HerbivoreCount, pB.HerbivoreCount);
            Assert.Equal(pA.CarnivoreCount, pB.CarnivoreCount);
            Assert.Equal(pA.BirthsThisWindow, pB.BirthsThisWindow);
            Assert.Equal(pA.DeathsThisWindow, pB.DeathsThisWindow);
            Assert.Equal(pA.HerbivoreTraits.AverageSpeed, pB.HerbivoreTraits.AverageSpeed);
            Assert.Equal(pA.CarnivoreTraits.AverageSpeed, pB.CarnivoreTraits.AverageSpeed);
        }
    }
}
