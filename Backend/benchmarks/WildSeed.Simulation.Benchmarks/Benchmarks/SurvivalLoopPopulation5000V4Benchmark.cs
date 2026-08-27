using BenchmarkDotNet.Attributes;
using WildSeed.Domain.World;
using WildSeed.Simulation.Benchmarks.Benchmarking;
using WildSeed.Simulation.Benchmarks.Workloads;
using WildSeed.Simulation.Engine;
using WildSeed.Simulation.WorldGeneration;

namespace WildSeed.Simulation.Benchmarks.Benchmarks;

[Config(typeof(PerformanceContractConfig))]
public class SurvivalLoopPopulation5000V4Benchmark
{
    private WorldMap _world = null!;

    [GlobalSetup]
    public void Setup() => _world = new WorldGenerator().Generate(new WorldConfiguration(
        seed: 42,
        width: 256,
        height: 256,
        initialHerbivores: SurvivalLoopPopulation5000V4Configuration.HerbivorePopulation,
        initialCarnivores: SurvivalLoopPopulation5000V4Configuration.CarnivorePopulation,
        vegetationDensity: 0.8f,
        waterLevel: 0.3f,
        mutationProbability: 0.05f,
        mutationStrength: 0.1f));

    [Benchmark(OperationsPerInvoke = SurvivalLoopPopulation5000V4Configuration.BatchTickCount)]
    public long RunBatch()
    {
        var engine = new SimulationEngine(SimulationStateFactory.Create(_world));
        engine.AdvanceTicks(SurvivalLoopPopulation5000V4Configuration.BatchTickCount);
        return engine.Snapshot().Tick + engine.Snapshot().Organisms.Count;
    }
}
