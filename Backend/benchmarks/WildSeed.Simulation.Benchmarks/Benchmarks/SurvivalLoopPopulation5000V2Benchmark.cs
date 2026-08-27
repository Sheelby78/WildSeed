using BenchmarkDotNet.Attributes;
using WildSeed.Domain.World;
using WildSeed.Simulation.Benchmarks.Benchmarking;
using WildSeed.Simulation.Benchmarks.Workloads;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.WorldGeneration;

namespace WildSeed.Simulation.Benchmarks.Benchmarks;

[Config(typeof(PerformanceContractConfig))]
public class SurvivalLoopPopulation5000V2Benchmark
{
    private WorldMap _world = null!;

    [GlobalSetup]
    public void Setup() => _world = new WorldGenerator().Generate(new WorldConfiguration(42, 256, 256, SurvivalLoopPopulation5000V2Configuration.PopulationSize, 0, 0.8f, 0.3f, 0, 0));

    [Benchmark(OperationsPerInvoke = SurvivalLoopPopulation5000V2Configuration.BatchTickCount)]
    public long RunBatch()
    {
        var engine = new SimulationEngine(SimulationStateFactory.Create(_world));
        engine.AdvanceTicks(SurvivalLoopPopulation5000V2Configuration.BatchTickCount);
        return engine.Snapshot().Tick + engine.Snapshot().Organisms.Count;
    }
}
