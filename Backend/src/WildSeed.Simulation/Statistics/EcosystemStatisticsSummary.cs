namespace WildSeed.Simulation.Statistics;

public sealed record EcosystemStatisticsSummary(
    int TotalPopulation,
    int Herbivores,
    int Carnivores,
    TraitStatistics OverallTraits,
    TraitStatistics HerbivoreTraits,
    TraitStatistics CarnivoreTraits,
    MortalityStatistics Mortality,
    int TotalBirths,
    int TotalDeaths,
    int WindowedBirths,
    int WindowedDeaths);
