using WildSeed.Simulation.Contracts;

namespace WildSeed.Simulation.Tests.Contracts;

public sealed class SimulationContractTests
{
    [Fact]
    public void Contract_version_is_4()
    {
        Assert.Equal(1, SimulationContract.Version1);
        Assert.Equal(2, SimulationContract.Version2);
        Assert.Equal(3, SimulationContract.Version3);
        Assert.Equal(4, SimulationContract.Version4);
        Assert.Equal(4, SimulationContract.CurrentVersion);
    }

    [Fact]
    public void SurvivalRulesV4_Constants_AreConfigured()
    {
        Assert.Equal(100, SurvivalRulesV4.MaturationAgeTicks);
        Assert.Equal(450, SurvivalRulesV4.MatingEnergyThreshold);
        Assert.Equal(150, SurvivalRulesV4.MatingCooldownTicks);
        Assert.Equal(150, SurvivalRulesV4.MatingEnergyCost);
        Assert.Equal(4, SurvivalRulesV4.HungerMetabolismCadenceTicks);
        Assert.Equal(5000, SurvivalRulesV4.MaxPopulationCap);
    }

    [Fact]
    public void OrganismBorn_Event_ContainsExpectedData()
    {
        var id = Guid.NewGuid();
        var mom = Guid.NewGuid();
        var dad = Guid.NewGuid();
        var genome = new Domain.Organisms.Genome(1.5f, 1.2f, 10.0f);
        var born = new Events.OrganismBorn(15, id, Domain.Organisms.Species.Herbivore, 10.0f, 20.0f, mom, dad, 2, genome);

        Assert.Equal(15, born.Tick);
        Assert.Equal(id, born.OrganismId);
        Assert.Equal(Domain.Organisms.Species.Herbivore, born.Species);
        Assert.Equal(10.0f, born.X);
        Assert.Equal(20.0f, born.Y);
        Assert.Equal(mom, born.MotherId);
        Assert.Equal(dad, born.FatherId);
        Assert.Equal(2, born.Generation);
        Assert.Equal(genome, born.Genome);
    }

    [Fact]
    public void Logical_tick_duration_is_100_milliseconds()
    {
        Assert.Equal(100, SimulationContract.LogicalTickMilliseconds);
        Assert.Equal(TimeSpan.FromMilliseconds(100), SimulationContract.LogicalTickDuration);
    }

    [Fact]
    public void Realtime_ticks_per_second_is_10()
    {
        Assert.Equal(10, SimulationContract.RealtimeTicksPerSecond);
        Assert.Equal(TimeSpan.FromSeconds(1), SimulationContract.LogicalTickDuration * SimulationContract.RealtimeTicksPerSecond);
    }

    [Fact]
    public void Acceptance_target_is_20x_realtime_and_200_ticks_per_second()
    {
        Assert.Equal(20.0, SimulationContract.TargetRealtimeMultiplier);
        Assert.Equal(200, SimulationContract.AcceptanceTicksPerSecond);
        Assert.Equal(
            SimulationContract.RealtimeTicksPerSecond * SimulationContract.TargetRealtimeMultiplier,
            SimulationContract.AcceptanceTicksPerSecond);
    }

    [Fact]
    public void Max_mean_tick_duration_for_acceptance_is_5_milliseconds()
    {
        Assert.Equal(5.0, SimulationContract.MaxMeanTickMillisecondsForAcceptance);
        Assert.Equal(
            (double)SimulationContract.LogicalTickMilliseconds / SimulationContract.TargetRealtimeMultiplier,
            SimulationContract.MaxMeanTickMillisecondsForAcceptance);
    }
}
