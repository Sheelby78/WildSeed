using WildSeed.Simulation.Contracts;

namespace WildSeed.Simulation.Tests.Contracts;

public sealed class SimulationContractTests
{
    [Fact]
    public void Contract_version_is_1()
    {
        Assert.Equal(1, SimulationContract.Version);
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
