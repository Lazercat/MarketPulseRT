using MarketPulseRT.Configuration;
using MarketPulseRT.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on both HTTP/1.1 and HTTP/2
builder.WebHost.ConfigureKestrel(options =>
{
    // gRPC requires HTTP/2. Use h2c (cleartext) locally; REST/health still work on HTTP/2.
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

// Configure market data options
builder.Services.Configure<MarketDataOptions>(
    builder.Configuration.GetSection(MarketDataOptions.SectionName));

builder.Services.AddGrpc();
builder.Services.AddHealthChecks();

// Register price stream based on configuration
var marketDataConfig = builder.Configuration
    .GetSection(MarketDataOptions.SectionName)
    .Get<MarketDataOptions>() ?? new MarketDataOptions();

switch (marketDataConfig.Source)
{
    case DataSourceType.Binance:
        builder.Services.AddSingleton<IPriceStream, BinanceWebSocketFeed>();
        builder.Services.AddHostedService(sp => (BinanceWebSocketFeed)sp.GetRequiredService<IPriceStream>());
        break;

    case DataSourceType.Mock:
    default:
        builder.Services.AddSingleton<IPriceStream, MarketPriceGenerator>();
        builder.Services.AddHostedService(sp => (MarketPriceGenerator)sp.GetRequiredService<IPriceStream>());
        break;
}

// Monitor and stats endpoint now work because the feed is fanned out per subscriber
builder.Services.AddSingleton<FeedMonitor>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<FeedMonitor>());

var app = builder.Build();

// Map gRPC service
app.MapGrpcService<MarketDataStreamerService>();

// Health check endpoint
app.MapHealthChecks("/health");

// Minimal info endpoint
app.MapGet("/", () => new
{
    Service = "MarketDataService",
    Description = "gRPC streaming market data",
    Source = marketDataConfig.Source.ToString(),
    Symbols = marketDataConfig.Symbols,
    Endpoints = new
    {
        Health = "/health",
        Stats = "/stats",
        GrpcService = "MarketDataStreamer.StreamTickers"
    }
});

// Stats endpoint to monitor feed (temporarily disabled - FeedMonitor not registered)
app.MapGet("/stats", (FeedMonitor monitor) =>
{
    var stats = monitor.GetStats();
    return new
    {
        Timestamp = DateTimeOffset.UtcNow,
        SymbolCount = stats.Count,
        Symbols = stats.Values.Select(s => new
        {
            s.Symbol,
            s.UpdateCount,
            s.LastPrice,
            s.LastUpdateTime,
            UpdateRate = $"{s.GetUpdateRate():F2}/sec"
        })
    };
});

// Simple test endpoint to verify service is running
app.MapGet("/test", () => new
{
    Status = "Running",
    Timestamp = DateTimeOffset.UtcNow,
    Message = "MarketDataService is operational. Data is streaming via gRPC (no stats available without FeedMonitor)."
});

app.Run();
