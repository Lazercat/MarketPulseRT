# Market Data Service

Real-time cryptocurrency market data streaming service using gRPC.

## Features

- **Real-time Binance data**: WebSocket stream from Binance exchange
- **Mock data generator**: For testing and development
- **Configurable sources**: Easy switch between data sources
- **Built-in monitoring**: Health checks and statistics endpoints
- **gRPC streaming**: High-performance server-side streaming

## Quick Start

```bash
# Start the service
dotnet run

# Or via yarn (from project root)
yarn dev:market-data
```

## Configuration

### Data Source Selection

Edit `appsettings.json` or use environment variables:

```json
{
  "MarketData": {
    "Source": "Binance",  // Options: "Mock", "Binance", "Alpaca"
    "Symbols": ["BTCUSDT", "ETHUSDT", "SOLUSDT"],
    "Binance": {
      "WebSocketUrl": "wss://stream.binance.com:9443",
      "ReconnectDelaySeconds": 5,
      "UseTestnet": false
    }
  }
}
```

### Environment Variables

Copy `.env.example` to `.env` and customize:

```bash
MarketData__Source=Binance
MarketData__Symbols__0=BTCUSDT
MarketData__Symbols__1=ETHUSDT
```

## Monitoring the Feed

### 1. Service Info Endpoint

```bash
curl http://localhost:5001/
```

Returns service configuration and available endpoints.

### 2. Health Check

```bash
curl http://localhost:5001/health
```

Returns `Healthy` when service is operational.

### 3. Statistics Dashboard

```bash
curl http://localhost:5001/stats
```

Returns real-time feed statistics:
```json
{
  "timestamp": "2025-11-25T18:30:00Z",
  "symbolCount": 3,
  "symbols": [
    {
      "symbol": "BTCUSDT",
      "updateCount": 1523,
      "lastPrice": 43251.50,
      "lastUpdateTime": "2025-11-25T18:30:00Z",
      "updateRate": "15.23/sec"
    }
  ]
}
```

### 4. Watch Stats in Real-Time

**Easy way - Use the helper script:**
```bash
cd apps/market-data-service
./watch-stats.sh
```

**Manual alternatives:**
```bash
# macOS (watch is not installed by default)
while true; do clear; curl -s http://localhost:5001/stats | jq; sleep 1; done

# Or install watch via Homebrew first
brew install watch
watch -n 1 'curl -s http://localhost:5001/stats | jq'

# Linux (watch is usually pre-installed)
watch -n 1 'curl -s http://localhost:5001/stats | jq'

# Without jq (plain JSON)
while true; do clear; curl -s http://localhost:5001/stats; echo; sleep 1; done
```

### 5. Application Logs

The service logs important events:
- First update received for each symbol
- Summary every 100 updates per symbol
- Connection status and errors
- Reconnection attempts

```bash
# Run with verbose logging
DOTNET_ENVIRONMENT=Development dotnet run
```

## Testing with grpcurl

Install `grpcurl`:
```bash
brew install grpcurl  # macOS
```

Test the gRPC service:
```bash
# Subscribe to all symbols
grpcurl -plaintext -d '{"symbol":""}' localhost:5001 marketdata.MarketDataStreamer/StreamTickers

# Subscribe to specific symbol
grpcurl -plaintext -d '{"symbol":"BTCUSDT"}' localhost:5001 marketdata.MarketDataStreamer/StreamTickers
```

## Data Sources

### Binance (Default)

- **Cost**: Free
- **Authentication**: None required for public data
- **Rate Limits**: No limits on WebSocket streams
- **Symbols**: All crypto pairs (BTC, ETH, SOL, etc.)
- **Update Frequency**: Real-time trades (~10-100/sec per symbol)

### Mock Data

- **Use Case**: Development and testing
- **Behavior**: Simulated price movements
- **Update Frequency**: 5 Hz per symbol (200ms interval)
- **Symbols**: Configurable

## Architecture

```
┌─────────────────────┐
│ IPriceStream        │  ← Interface
│ (Abstraction)       │
└──────────┬──────────┘
           │
     ┌─────┴──────┐
     │            │
┌────▼────┐  ┌───▼──────────────┐
│ Mock    │  │ Binance          │
│ Generator│  │ WebSocket Feed   │
└─────────┘  └──────────────────┘
     │            │
     └────┬───────┘
          │
     ┌────▼────────────┐
     │ FeedMonitor     │  ← Statistics
     └─────────────────┘
          │
     ┌────▼────────────┐
     │ gRPC Service    │  ← Client API
     └─────────────────┘
```

## Best Practices for Monitoring

1. **Start with /stats**: Quick overview of feed health
2. **Check update rates**: Should be >0 for all symbols
3. **Monitor logs**: Watch for connection errors or gaps
4. **Use health endpoint**: In production monitoring
5. **Test with grpcurl**: Verify data is flowing

## Troubleshooting

**No updates in /stats?**
- Check network connectivity
- Verify Binance is not blocked
- Check application logs for errors

**Low update rates?**
- Normal for less liquid pairs
- BTC usually has highest rate

**Connection errors?**
- Service auto-reconnects after 5 seconds
- Check WebSocketUrl configuration

## Production Deployment

1. **Use HTTPS**: Configure TLS for production
2. **Monitor /health**: Set up health check polling
3. **Log aggregation**: Send logs to centralized system
4. **Metrics**: Export /stats to monitoring dashboard
5. **Alerting**: Alert on health check failures

## Adding New Data Sources

1. Implement `IPriceStream` interface
2. Add configuration to `MarketDataOptions`
3. Register in `Program.cs` switch statement
4. Test with mock data first

See `BinanceWebSocketFeed.cs` as reference implementation.
