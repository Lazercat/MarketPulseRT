using Microsoft.AspNetCore.Mvc;

namespace MarketPulseRT.Controllers;

[ApiController]
[Route("api/market")]
public class MarketSnapshotController : ControllerBase
{
    [HttpGet("tickers")]
    public IActionResult GetTickers()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var data = new[]
        {
            new { symbol = "BTCUSDT", lastPrice = 43000.5, bidPrice = 42995.0, askPrice = 43005.0, tsUnixMs = now },
            new { symbol = "ETHUSDT", lastPrice = 3200.1, bidPrice = 3198.0,  askPrice = 3202.0,  tsUnixMs = now },
            new { symbol = "SOLUSDT", lastPrice = 160.25, bidPrice = 159.8,  askPrice = 160.7,  tsUnixMs = now }
        };

        return Ok(data);
    }
}
