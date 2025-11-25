using System.Collections.Concurrent;

namespace MarketPulseRT.Services;

public class MarketPriceGenerator : PriceStreamBroadcaster
{
    private readonly ConcurrentDictionary<string, decimal> _prices = new();

    private readonly string[] _symbols = new[] { "BTCUSDT", "ETHUSDT", "SOLUSDT" };
    private readonly Random _rng = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initialize prices
        foreach (var s in _symbols)
        {
            _prices[s] = _rng.Next(20000, 60000);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var symbol in _symbols)
            {
                var delta = (decimal)(_rng.NextDouble() - 0.5) * 50m;
                var newPrice = Math.Max(0, _prices[symbol] + delta);
                _prices[symbol] = newPrice;

                Broadcast(symbol, newPrice);
            }

            await Task.Delay(200, stoppingToken); // ~5Hz per symbol
        }
    }
}
