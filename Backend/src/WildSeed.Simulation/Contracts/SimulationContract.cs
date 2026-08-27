namespace WildSeed.Simulation.Contracts;

public static class SimulationContract
{
    public const int Version1 = 1;

    public const int Version2 = 2;

    public const int Version3 = 3;

    public const int Version4 = 4;

    public const int CurrentVersion = Version4;

    public const int LogicalTickMilliseconds = 100;

    public static readonly TimeSpan LogicalTickDuration = TimeSpan.FromMilliseconds(LogicalTickMilliseconds);

    public const int RealtimeTicksPerSecond = 1000 / LogicalTickMilliseconds;

    public const double TargetRealtimeMultiplier = 20.0;

    public const int AcceptanceTicksPerSecond = (int)(RealtimeTicksPerSecond * TargetRealtimeMultiplier);
    
    public const double MaxMeanTickMillisecondsForAcceptance = (double)LogicalTickMilliseconds / TargetRealtimeMultiplier;
}
