using WildSeed.Domain.Organisms;
using WildSeed.Domain.Terrain;
using WildSeed.Domain.World;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.Events;
using WildSeed.Simulation.Statistics;
using Xunit;

namespace WildSeed.Simulation.Tests.Statistics;

public sealed class EcosystemStatisticsTrackerTests
{
    private static SimulationState CreateEmptyState()
    {
        var tiles = new Tile[64, 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
            tiles[x, y] = new Tile(x, y, TerrainType.Grass, 0.5f);

        var config = new WorldConfiguration(
            seed: 42,
            width: 64,
            height: 64,
            initialHerbivores: 0,
            initialCarnivores: 0,
            vegetationDensity: 0.5f,
            waterLevel: 0.1f,
            mutationProbability: 0.1f,
            mutationStrength: 0.1f);

        var world = new WorldMap(config, tiles, []);
        return SimulationStateFactory.Create(world);
    }

    [Fact]
    public void GetSummary_EmptyState_ReturnsZeroSafely()
    {
        var tracker = new EcosystemStatisticsTracker(capacity: 10, sampleCadenceTicks: 5);
        var state = CreateEmptyState();

        var summary = tracker.GetSummary(state);

        Assert.Equal(0, summary.TotalPopulation);
        Assert.Equal(0, summary.Herbivores);
        Assert.Equal(0, summary.Carnivores);
        Assert.Equal(0f, summary.OverallTraits.AverageSpeed);
        Assert.Equal(0f, summary.OverallTraits.AverageSize);
        Assert.Equal(0f, summary.OverallTraits.AverageVision);
        Assert.Equal(0f, summary.HerbivoreTraits.AverageSpeed);
        Assert.Equal(0f, summary.CarnivoreTraits.AverageSpeed);
        Assert.Equal(0, summary.Mortality.TotalDeaths);
        Assert.Equal(0f, summary.Mortality.AverageLifespanTicks);
        Assert.Equal(0f, summary.Mortality.MaxLifespanTicks);
        Assert.Equal(0, summary.TotalBirths);
        Assert.Equal(0, summary.TotalDeaths);
    }

    [Fact]
    public void ComputeTraitStatistics_CalculatesAveragesCorrectly()
    {
        var tracker = new EcosystemStatisticsTracker();
        var organisms = new List<OrganismState>
        {
            new(Guid.NewGuid(), Species.Herbivore, new Genome(1.0f, 2.0f, 8.0f), 0, 0),
            new(Guid.NewGuid(), Species.Herbivore, new Genome(3.0f, 4.0f, 12.0f), 0, 0),
            new(Guid.NewGuid(), Species.Carnivore, new Genome(2.0f, 1.0f, 6.0f), 0, 0)
        };

        var overall = tracker.ComputeTraitStatistics(organisms);
        var herb = tracker.ComputeTraitStatistics(organisms, Species.Herbivore);
        var carn = tracker.ComputeTraitStatistics(organisms, Species.Carnivore);

        Assert.Equal(2.0f, overall.AverageSpeed, precision: 3);
        Assert.Equal(2.333f, overall.AverageSize, precision: 3);
        Assert.Equal(8.667f, overall.AverageVision, precision: 3);

        Assert.Equal(2.0f, herb.AverageSpeed, precision: 3);
        Assert.Equal(3.0f, herb.AverageSize, precision: 3);
        Assert.Equal(10.0f, herb.AverageVision, precision: 3);

        Assert.Equal(2.0f, carn.AverageSpeed, precision: 3);
        Assert.Equal(1.0f, carn.AverageSize, precision: 3);
        Assert.Equal(6.0f, carn.AverageVision, precision: 3);
    }

    [Fact]
    public void RecordDeaths_UpdatesMortalityStatisticsAndLifespans()
    {
        var tracker = new EcosystemStatisticsTracker();
        var deaths = new List<OrganismDied>
        {
            new(10, Guid.NewGuid(), Species.Herbivore, DeathCause.Starvation, 1f, 1f, AgeTicks: 100),
            new(10, Guid.NewGuid(), Species.Herbivore, DeathCause.Predation, 2f, 2f, AgeTicks: 200),
            new(10, Guid.NewGuid(), Species.Carnivore, DeathCause.OldAge, 3f, 3f, AgeTicks: 600)
        };

        tracker.RecordDeaths(deaths);

        var mortality = tracker.GetMortalityStatistics();
        Assert.Equal(3, mortality.TotalDeaths);
        Assert.Equal(1, mortality.DeathsByCause["Starvation"]);
        Assert.Equal(1, mortality.DeathsByCause["Predation"]);
        Assert.Equal(1, mortality.DeathsByCause["OldAge"]);
        Assert.Equal(300f, mortality.AverageLifespanTicks);
        Assert.Equal(600f, mortality.MaxLifespanTicks);
        Assert.Equal(150f, mortality.HerbivoreAverageLifespanTicks);
        Assert.Equal(600f, mortality.CarnivoreAverageLifespanTicks);
    }

    [Fact]
    public void SampleTick_RecordsHistoryAtCadence()
    {
        var tracker = new EcosystemStatisticsTracker(capacity: 5, sampleCadenceTicks: 10);
        var state = CreateEmptyState();
        state.Organisms.Add(new OrganismState(Guid.NewGuid(), Species.Herbivore, new Genome(1.5f), 1f, 1f));

        state.Tick = 9;
        tracker.SampleTick(state);
        Assert.Empty(tracker.History);

        state.Tick = 10;
        tracker.RecordBirths([new OrganismBorn(10, Guid.NewGuid(), Species.Herbivore, 1f, 1f, null, null, 1, new Genome(1.5f))]);
        tracker.SampleTick(state);

        Assert.Single(tracker.History);
        var point = tracker.History[0];
        Assert.Equal(10, point.Tick);
        Assert.Equal(1, point.TotalPopulation);
        Assert.Equal(1, point.HerbivoreCount);
        Assert.Equal(0, point.CarnivoreCount);
        Assert.Equal(1, point.BirthsThisWindow);
        Assert.Equal(0, point.DeathsThisWindow);
        Assert.Equal(1.5f, point.HerbivoreTraits.AverageSpeed);
    }
}
