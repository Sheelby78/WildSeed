namespace WildSeed.Simulation.Benchmarks.Workloads;

public static class SurvivalLoopPopulation5000V4Configuration
{
    public const int HerbivorePopulation = 4_500;
    public const int CarnivorePopulation = 500;
    public const int TotalPopulation = HerbivorePopulation + CarnivorePopulation;
    public const int BatchTickCount = 20;
}
