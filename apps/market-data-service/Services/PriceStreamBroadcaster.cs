using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MarketPulseRT.Services;

/// <summary>
/// Base class that fans out price ticks to any number of subscribers.
/// </summary>
public abstract class PriceStreamBroadcaster : BackgroundService, IPriceStream
{
    private readonly ConcurrentDictionary<Guid, Channel<(string Symbol, decimal Price)>> _subscribers = new();

    protected void Broadcast(string symbol, decimal price)
    {
        var snapshot = _subscribers.ToArray();
        foreach (var (_, channel) in snapshot)
        {
            // Drop if the subscriber has gone away; removal happens on dispose.
            channel.Writer.TryWrite((symbol, price));
        }
    }

    public async IAsyncEnumerable<(string Symbol, decimal Price)> GetStream(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<(string Symbol, decimal Price)>();
        var id = Guid.NewGuid();
        _subscribers[id] = channel;

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(ct))
            {
                yield return item;
            }
        }
        finally
        {
            if (_subscribers.TryRemove(id, out var ch))
            {
                ch.Writer.TryComplete();
            }
        }
    }

    protected void CompleteSubscribers(Exception? error = null)
    {
        foreach (var channel in _subscribers.Values)
        {
            channel.Writer.TryComplete(error);
        }
    }
}
