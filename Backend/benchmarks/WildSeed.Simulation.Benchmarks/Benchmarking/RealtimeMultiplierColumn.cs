using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using WildSeed.Simulation.Contracts;

namespace WildSeed.Simulation.Benchmarks.Benchmarking;

public sealed class RealtimeMultiplierColumn : IColumn
{
    public string Id => nameof(RealtimeMultiplierColumn);
    public string ColumnName => "Realtime Multiple";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 1;
    public bool IsNumeric => true;
    public UnitType UnitType => UnitType.Dimensionless;
    public string Legend => "Simulated realtime speed multiple achieved (Target >= 20.0x)";

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        var report = summary[benchmarkCase];
        if (report?.ResultStatistics is null || report.ResultStatistics.Mean <= 0)
        {
            return "N/A";
        }

        double meanNsPerOp = report.ResultStatistics.Mean;
        double meanMsPerOp = meanNsPerOp / 1_000_000.0;
        double multiplier = SimulationContract.LogicalTickMilliseconds / meanMsPerOp;

        return $"{multiplier:F1}x";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) =>
        GetValue(summary, benchmarkCase);

    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
}
