using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MarketPulseRT.Configuration;
using Microsoft.Extensions.Options;

namespace MarketPulseRT.Services;

public class BinanceWebSocketFeed : PriceStreamBroadcaster
{
    private readonly ILogger<BinanceWebSocketFeed> _logger;
    private readonly MarketDataOptions _options;
    private ClientWebSocket? _webSocket;

    public BinanceWebSocketFeed(
        ILogger<BinanceWebSocketFeed> logger,
        IOptions<MarketDataOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndStreamAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error in Binance WebSocket feed. Reconnecting in {Delay}s...",
                    _options.Binance?.ReconnectDelaySeconds ?? 5);

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.Binance?.ReconnectDelaySeconds ?? 5),
                    stoppingToken);
            }
        }
    }

    private async Task ConnectAndStreamAsync(CancellationToken ct)
    {
        _webSocket?.Dispose();
        _webSocket = new ClientWebSocket();

        // Build stream names for all symbols (e.g., btcusdt@trade) and de-duplicate in case config repeats
        var streams = string.Join("/",
            _options.Symbols
                .Select(s => $"{s.ToLowerInvariant()}@trade")
                .Distinct(StringComparer.OrdinalIgnoreCase));

        // Allow testnet override to avoid geo-blocks
        var baseUrl = _options.Binance?.UseTestnet == true
            ? "wss://testnet.binance.vision"
            : _options.Binance?.WebSocketUrl ?? "wss://stream.binance.com:9443";

        var url = $"{baseUrl}/stream?streams={streams}";

        _logger.LogInformation("Connecting to Binance WebSocket: {Url}", url);
        await _webSocket.ConnectAsync(new Uri(url), ct);
        _logger.LogInformation("Connected to Binance WebSocket");

        var buffer = new byte[8192];
        var messageBuffer = new StringBuilder();

        while (_webSocket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogWarning("Binance WebSocket closed by server");
                break;
            }

            messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (result.EndOfMessage)
            {
                try
                {
                    ProcessMessage(messageBuffer.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Binance message: {Message}",
                        messageBuffer.ToString());
                }
                finally
                {
                    messageBuffer.Clear();
                }
            }
        }
    }

    private void ProcessMessage(string message)
    {
        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;

        // Binance combined stream format: {"stream":"btcusdt@trade","data":{...}}
        if (!root.TryGetProperty("data", out var data))
            return;

        if (!data.TryGetProperty("s", out var symbolProp))  // Symbol
            return;

        if (!data.TryGetProperty("p", out var priceProp))   // Price
            return;

        var symbol = symbolProp.GetString();
        var priceStr = priceProp.GetString();

        if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(priceStr))
            return;

        if (!decimal.TryParse(priceStr, out var price))
            return;

        Broadcast(symbol.ToUpperInvariant(), price);
    }

    public override void Dispose()
    {
        CompleteSubscribers();
        _webSocket?.Dispose();
        base.Dispose();
    }
}
