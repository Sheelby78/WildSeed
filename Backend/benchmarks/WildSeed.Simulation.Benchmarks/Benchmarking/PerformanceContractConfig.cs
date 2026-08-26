using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;

namespace WildSeed.Simulation.Benchmarks.Benchmarking;

public sealed class PerformanceContractConfig : ManualConfig
{
    public PerformanceContractConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
        AddColumn(new RealtimeMultiplierColumn());
        AddColumn(new TicksPerSecondColumn());
    }
}
