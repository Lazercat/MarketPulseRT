namespace MarketPulseRT.Configuration;

public class MarketDataOptions
{
    public const string SectionName = "MarketData";

    public DataSourceType Source { get; set; } = DataSourceType.Mock;
    public string[] Symbols { get; set; } = ["BTCUSDT", "ETHUSDT", "SOLUSDT"];
    public BinanceOptions? Binance { get; set; }
}

public enum DataSourceType
{
    Mock,
    Binance,
    Alpaca
}

public class BinanceOptions
{
    public string WebSocketUrl { get; set; } = "wss://stream.binance.com:9443";
    public int ReconnectDelaySeconds { get; set; } = 5;
    public bool UseTestnet { get; set; } = false;
}
