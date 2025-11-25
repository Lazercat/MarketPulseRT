using Grpc.Core;
using MarketPulseRT.Proto;

namespace MarketPulseRT.Services;

public class MarketDataStreamerService : MarketDataStreamer.MarketDataStreamerBase
{
    private readonly IPriceStream _priceStream;

    public MarketDataStreamerService(IPriceStream priceStream)
    {
        _priceStream = priceStream;
    }

    public override async Task StreamTickers(
        TickerSubscription request,
        IServerStreamWriter<TickerUpdate> responseStream,
        ServerCallContext context)
    {
        var requestedSymbol = request.Symbol?.Trim();
        var symbolFilter = string.IsNullOrWhiteSpace(requestedSymbol)
            ? null
            : requestedSymbol.ToUpperInvariant();

        await foreach (var (symbol, price) in _priceStream.GetStream(context.CancellationToken))
        {
            if (symbolFilter != null &&
                !symbol.Equals(symbolFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var last = price;
            var bid = last - 5;
            var ask = last + 5;

            await responseStream.WriteAsync(new TickerUpdate
            {
                Symbol = symbol,
                LastPrice = (double)last,
                BidPrice = (double)bid,
                AskPrice = (double)ask,
                TsUnixMs = ts
            });
        }
    }
}
