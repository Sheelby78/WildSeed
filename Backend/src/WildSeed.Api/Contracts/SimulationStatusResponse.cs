namespace WildSeed.Api.Contracts;

public sealed record SimulationStatusResponse(long Tick, bool IsRunning, string Speed, string Fingerprint, int Population);
