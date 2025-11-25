using Grpc.Core;
using Grpc.Net.Client;
using MarketPulseRT.Hubs;
using MarketPulseRT.Proto;
using Microsoft.AspNetCore.SignalR;

namespace MarketPulseRT.Services;

public class MarketStreamerRelay : BackgroundService
{
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly ILogger<MarketStreamerRelay> _logger;
    private readonly IConfiguration _config;

    public MarketStreamerRelay(
        IHubContext<MarketHub> hubContext,
        ILogger<MarketStreamerRelay> logger,
        IConfiguration config)
    {
        _hubContext = hubContext;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var grpcUrl = _config["MarketDataService:GrpcUrl"] ?? "http://localhost:5001";

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Connecting to MarketDataService at {Url}", grpcUrl);

            try
            {
                var handler = new SocketsHttpHandler
                {
                    KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                    EnableMultipleHttp2Connections = true,
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    AllowAutoRedirect = false
                };

                var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
                {
                    HttpHandler = handler
                });
                var client = new MarketDataStreamer.MarketDataStreamerClient(channel);

                // Empty symbol => all symbols from generator
                var sub = new TickerSubscription { Symbol = "" };

                using var call = client.StreamTickers(sub, cancellationToken: stoppingToken);
                _logger.LogInformation("gRPC stream established. Relaying ticks to SignalR clients...");

                await foreach (var update in call.ResponseStream.ReadAllAsync(cancellationToken: stoppingToken))
                {
                    var symbolGroup = update.Symbol.ToUpperInvariant();

                    await _hubContext
                        .Clients
                        .Group(symbolGroup)
                        .SendAsync("tickerUpdate", update, cancellationToken: stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error streaming tickers. Retrying shortly...");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
