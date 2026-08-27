namespace WildSeed.Simulation.Statistics;

public sealed record MortalityStatistics(
    int TotalDeaths,
    IReadOnlyDictionary<string, int> DeathsByCause,
    float AverageLifespanTicks,
    float MaxLifespanTicks,
    float HerbivoreAverageLifespanTicks,
    float CarnivoreAverageLifespanTicks);
