namespace WildSeed.Api.Contracts;

public sealed record GenerateWorldResponse(string SessionToken, WorldSnapshotResponse StaticWorld, SimulationSnapshotResponse Snapshot);
