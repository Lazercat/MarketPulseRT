using System.Collections.Concurrent;
using System.Linq;
using MarketPulseRT.Configuration;
using Microsoft.Extensions.Options;

namespace MarketPulseRT.Services;

public class MarketPriceGenerator : PriceStreamBroadcaster
{
    private readonly ConcurrentDictionary<string, decimal> _prices = new();

    private readonly string[] _symbols;
    private readonly Random _rng = new();

    public MarketPriceGenerator(IOptions<MarketDataOptions> options)
    {
        var configuredSymbols = options.Value.Symbols ?? Array.Empty<string>();
        _symbols = configuredSymbols.Length > 0
            ? configuredSymbols
                .Select(s => s.ToUpperInvariant())
                .Distinct()
                .ToArray()
            : new[] { "BTCUSDT", "ETHUSDT", "SOLUSDT", "AVAXUSDT", "DOGEUSDT" };
    }

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
