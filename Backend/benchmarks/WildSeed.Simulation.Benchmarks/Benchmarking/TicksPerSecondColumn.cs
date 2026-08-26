using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace WildSeed.Simulation.Benchmarks.Benchmarking;

public sealed class TicksPerSecondColumn : IColumn
{
    public string Id => nameof(TicksPerSecondColumn);
    public string ColumnName => "Ticks/sec";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 2;
    public bool IsNumeric => true;
    public UnitType UnitType => UnitType.Dimensionless;
    public string Legend => "Simulation ticks processed per wall-clock second (Target >= 200)";

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        var report = summary[benchmarkCase];
        if (report?.ResultStatistics is null || report.ResultStatistics.Mean <= 0)
        {
            return "N/A";
        }

        double meanNsPerOp = report.ResultStatistics.Mean;
        double ticksPerSec = 1_000_000_000.0 / meanNsPerOp;

        return $"{ticksPerSec:F0}";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) =>
        GetValue(summary, benchmarkCase);

    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
}
