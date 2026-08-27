namespace WildSeed.Simulation.Statistics;

public sealed record SimulationHistoryPoint(
    long Tick,
    int TotalPopulation,
    int HerbivoreCount,
    int CarnivoreCount,
    int BirthsThisWindow,
    int DeathsThisWindow,
    TraitStatistics HerbivoreTraits,
    TraitStatistics CarnivoreTraits);
