namespace WildSeed.Api.Contracts;

public sealed record SimulationCommandResult(bool Success, string? Error, SimulationStatusResponse? Status);
