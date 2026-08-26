namespace WildSeed.Simulation.Benchmarks.Workloads;

public static class SyntheticPopulation5000V1Configuration
{
    public const int PopulationSize = 5000;
    public const int BatchTickCount = 200;
    public const ulong DefaultSeed = 42UL;
    public const int WorldWidth = 1000;
    public const int WorldHeight = 1000;
    public const int GridCellSize = 50;
    public const int GridCols = WorldWidth / GridCellSize;
    public const int GridRows = WorldHeight / GridCellSize;
}
