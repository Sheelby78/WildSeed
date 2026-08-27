using WildSeed.Api.Contracts;

namespace WildSeed.Api.Hubs;

public interface ISimulationClient
{
    Task Snapshot(SimulationSnapshotResponse snapshot);
    Task Fault(string code);
}
