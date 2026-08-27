using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using WildSeed.Api.Contracts;
using WildSeed.Api.Hubs;

namespace WildSeed.Api.SimulationHosting;

public sealed class LatestSnapshotMailbox : IAsyncDisposable
{
    private readonly string _token;
    private readonly IHubContext<SimulationHub, ISimulationClient> _hub;
    private readonly Channel<SimulationSnapshotResponse> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _consumerTask;

    public LatestSnapshotMailbox(string token, IHubContext<SimulationHub, ISimulationClient> hub)
    {
        _token = token;
        _hub = hub;
        _channel = Channel.CreateBounded<SimulationSnapshotResponse>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        _consumerTask = Task.Run(ConsumeAsync);
    }

    public void Post(SimulationSnapshotResponse snapshot)
    {
        _channel.Writer.TryWrite(snapshot);
    }

    private async Task ConsumeAsync()
    {
        var reader = _channel.Reader;
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                if (await reader.WaitToReadAsync(_cts.Token))
                {
                    while (reader.TryRead(out var snapshot))
                    {
                        await _hub.Clients.Group(_token).Snapshot(snapshot);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Isolate network/client faults from crashing mailbox
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _channel.Writer.TryComplete();
        try
        {
            await _consumerTask;
        }
        catch
        {
            // Ignore termination exceptions on dispose
        }
        _cts.Dispose();
    }
}
