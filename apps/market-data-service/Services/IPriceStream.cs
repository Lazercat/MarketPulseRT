using System.Collections.Generic;

namespace MarketPulseRT.Services;

public interface IPriceStream
{
    IAsyncEnumerable<(string Symbol, decimal Price)> GetStream(CancellationToken ct);
}
