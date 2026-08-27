namespace WildSeed.Api.SimulationHosting;

public sealed class SimulationHostOptions
{
    public int RetentionSeconds { get; init; } = 60;
    public int PublicationHz { get; init; } = 15;
}
