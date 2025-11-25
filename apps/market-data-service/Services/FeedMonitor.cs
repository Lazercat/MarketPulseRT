using System.Collections.Concurrent;

namespace MarketPulseRT.Services;

/// <summary>
/// Monitors the price stream and provides diagnostic information
/// </summary>
public class FeedMonitor : BackgroundService
{
    private readonly IPriceStream _priceStream;
    private readonly ILogger<FeedMonitor> _logger;
    private readonly ConcurrentDictionary<string, SymbolStats> _stats = new();

    public FeedMonitor(IPriceStream priceStream, ILogger<FeedMonitor> logger)
    {
        _priceStream = priceStream;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Feed monitor starting...");

        await foreach (var (symbol, price) in _priceStream.GetStream(stoppingToken))
        {
            var stats = _stats.GetOrAdd(symbol, _ => new SymbolStats { Symbol = symbol });
            stats.UpdateCount++;
            stats.LastPrice = price;
            stats.LastUpdateTime = DateTimeOffset.UtcNow;

            if (stats.UpdateCount == 1)
            {
                _logger.LogInformation("First update received for {Symbol}: {Price}", symbol, price);
            }

            // Log summary every 100 updates per symbol
            if (stats.UpdateCount % 100 == 0)
            {
                _logger.LogInformation(
                    "{Symbol}: {Count} updates, Last: {Price}, Rate: {Rate:F2}/sec",
                    symbol,
                    stats.UpdateCount,
                    price,
                    stats.GetUpdateRate());
            }
        }
    }

    public IReadOnlyDictionary<string, SymbolStats> GetStats() => _stats;
}

public class SymbolStats
{
    public required string Symbol { get; init; }
    public long UpdateCount { get; set; }
    public decimal LastPrice { get; set; }
    public DateTimeOffset FirstUpdateTime { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastUpdateTime { get; set; }

    public double GetUpdateRate()
    {
        var elapsed = (LastUpdateTime - FirstUpdateTime).TotalSeconds;
        return elapsed > 0 ? UpdateCount / elapsed : 0;
    }
}
