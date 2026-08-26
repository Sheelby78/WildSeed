using BenchmarkDotNet.Attributes;
using WildSeed.Simulation.Benchmarks.Benchmarking;
using WildSeed.Simulation.Benchmarks.Workloads;

namespace WildSeed.Simulation.Benchmarks.Benchmarks;

[Config(typeof(PerformanceContractConfig))]
public class SyntheticPopulation5000V1Benchmark
{
    private SyntheticPopulation5000V1Scenario _scenario = null!;

    [GlobalSetup]
    public void Setup()
    {
        _scenario = new SyntheticPopulation5000V1Scenario(
            seed: SyntheticPopulation5000V1Configuration.DefaultSeed,
            population: SyntheticPopulation5000V1Configuration.PopulationSize);
    }

    [Benchmark(OperationsPerInvoke = SyntheticPopulation5000V1Configuration.BatchTickCount)]
    public long RunBatch()
    {
        return _scenario.Step(SyntheticPopulation5000V1Configuration.BatchTickCount);
    }
}
