using WildSeed.Simulation.Statistics;

namespace WildSeed.Api.Contracts;

public sealed record TraitStatisticsDto(
    float AverageSpeed,
    float AverageSize,
    float AverageVision)
{
    public static TraitStatisticsDto FromDomain(TraitStatistics stats) =>
        new(stats.AverageSpeed, stats.AverageSize, stats.AverageVision);
}

public sealed record MortalityStatisticsDto(
    int TotalDeaths,
    IReadOnlyDictionary<string, int> DeathsByCause,
    float AverageLifespanTicks,
    float MaxLifespanTicks,
    float HerbivoreAverageLifespanTicks,
    float CarnivoreAverageLifespanTicks)
{
    public static MortalityStatisticsDto FromDomain(MortalityStatistics stats) =>
        new(stats.TotalDeaths,
            stats.DeathsByCause,
            stats.AverageLifespanTicks,
            stats.MaxLifespanTicks,
            stats.HerbivoreAverageLifespanTicks,
            stats.CarnivoreAverageLifespanTicks);
}

public sealed record SimulationHistoryPointDto(
    long Tick,
    int TotalPopulation,
    int HerbivoreCount,
    int CarnivoreCount,
    int BirthsThisWindow,
    int DeathsThisWindow,
    TraitStatisticsDto HerbivoreTraits,
    TraitStatisticsDto CarnivoreTraits)
{
    public static SimulationHistoryPointDto FromDomain(SimulationHistoryPoint point) =>
        new(point.Tick,
            point.TotalPopulation,
            point.HerbivoreCount,
            point.CarnivoreCount,
            point.BirthsThisWindow,
            point.DeathsThisWindow,
            TraitStatisticsDto.FromDomain(point.HerbivoreTraits),
            TraitStatisticsDto.FromDomain(point.CarnivoreTraits));
}

public sealed record EcosystemStatisticsSummaryDto(
    int TotalPopulation,
    int Herbivores,
    int Carnivores,
    TraitStatisticsDto OverallTraits,
    TraitStatisticsDto HerbivoreTraits,
    TraitStatisticsDto CarnivoreTraits,
    MortalityStatisticsDto Mortality,
    int TotalBirths,
    int TotalDeaths,
    int WindowedBirths,
    int WindowedDeaths)
{
    public static EcosystemStatisticsSummaryDto FromDomain(EcosystemStatisticsSummary summary) =>
        new(summary.TotalPopulation,
            summary.Herbivores,
            summary.Carnivores,
            TraitStatisticsDto.FromDomain(summary.OverallTraits),
            TraitStatisticsDto.FromDomain(summary.HerbivoreTraits),
            TraitStatisticsDto.FromDomain(summary.CarnivoreTraits),
            MortalityStatisticsDto.FromDomain(summary.Mortality),
            summary.TotalBirths,
            summary.TotalDeaths,
            summary.WindowedBirths,
            summary.WindowedDeaths);
}
