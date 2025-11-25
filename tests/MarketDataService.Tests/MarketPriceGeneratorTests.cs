using MarketPulseRT.Services;

namespace MarketDataService.Tests;

public class MarketPriceGeneratorTests
{
    [Fact]
    public async Task EmitsPricesForConfiguredSymbols()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var generator = new MarketPriceGenerator();

        var updates = new List<(string Symbol, decimal Price)>();
        await foreach (var update in generator.GetStream(cts.Token))
        {
            updates.Add(update);
            if (updates.Count >= 5)
            {
                break;
            }
        }

        Assert.NotEmpty(updates);
        Assert.All(updates, u => Assert.False(string.IsNullOrWhiteSpace(u.Symbol)));
        Assert.All(updates, u => Assert.True(u.Price > 0));
    }
}
