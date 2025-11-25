import { useEffect, useMemo, useRef, useState } from "react";
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { AgGridReact } from "ag-grid-react";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import type { ColDef } from "ag-grid-community";
import { format } from "date-fns";
import { Card, Pill } from "@app/design-system";
import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-quartz.css";
import "./App.css";

// Register all community features for AG Grid v33 module build
ModuleRegistry.registerModules([AllCommunityModule]);

type TickerUpdate = {
  symbol: string;
  lastPrice: number;
  bidPrice: number;
  askPrice: number;
  tsUnixMs: number;
};

type TickerRow = TickerUpdate & { change: number };
type ConnectionState = "disconnected" | "connecting" | "connected" | "reconnecting";

const DEFAULT_SYMBOLS = ["BTCUSDT", "ETHUSDT", "SOLUSDT", "AVAXUSDT", "DOGEUSDT"];

function useTickerStream(symbols: string[]) {
  const [rows, setRows] = useState<TickerRow[]>([]);
  const [connectionState, setConnectionState] = useState<ConnectionState>("disconnected");
  const [lastError, setLastError] = useState<string | null>(null);
  const [metrics, setMetrics] = useState({
    totalUpdates: 0,
    updatesPerSec: 0,
    connectedAt: 0,
  });
  const [history, setHistory] = useState<Record<string, Array<{ ts: number; price: number }>>>({});

  const bufferRef = useRef<Record<string, TickerRow>>({});
  const historyRef = useRef<Record<string, Array<{ ts: number; price: number }>>>({});
  const totalUpdatesRef = useRef(0);
  const lastFlushCountRef = useRef(0);
  const lastFlushTimeRef = useRef(Date.now());
  const rafRef = useRef<number | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    let isMounted = true;

    const flush = () => {
      rafRef.current = null;
      const now = Date.now();
      const nextRows = Object.values(bufferRef.current).sort(
        (a, b) => b.tsUnixMs - a.tsUnixMs
      );

      const delta = totalUpdatesRef.current - lastFlushCountRef.current;
      const elapsed = (now - lastFlushTimeRef.current) / 1000;
      const updatesPerSec = elapsed > 0 ? delta / elapsed : 0;

      lastFlushCountRef.current = totalUpdatesRef.current;
      lastFlushTimeRef.current = now;

      if (!isMounted) return;

      setRows(nextRows);
      setMetrics((prev) => ({
        ...prev,
        totalUpdates: totalUpdatesRef.current,
        updatesPerSec,
      }));
      setHistory({ ...historyRef.current });
    };

    const scheduleFlush = () => {
      if (rafRef.current !== null) return;
      rafRef.current = requestAnimationFrame(flush);
    };

    const connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5100/hubs/market")
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.Information)
      .build();

    connectionRef.current = connection;

    connection.on("tickerUpdate", (update: TickerUpdate) => {
      const prev = bufferRef.current[update.symbol];
      const change = prev ? update.lastPrice - prev.lastPrice : 0;
      bufferRef.current[update.symbol] = { ...update, change };

       // Keep a bounded rolling price history for sparklines (last ~90 points)
      const existing = historyRef.current[update.symbol] ?? [];
      const nextHistory = [...existing, { ts: update.tsUnixMs, price: update.lastPrice }];
      // Limit to 90 samples to keep render light
      historyRef.current[update.symbol] = nextHistory.slice(-90);

      totalUpdatesRef.current += 1;
      scheduleFlush();
    });

    connection.onreconnecting((error) => {
      if (!isMounted) return;
      setConnectionState("reconnecting");
      setLastError(error?.message ?? null);
    });

    connection.onreconnected(async () => {
      if (!isMounted) return;
      setConnectionState("connected");
      setLastError(null);
      await Promise.all(symbols.map((s) => connection.invoke("Subscribe", s)));
    });

    connection.onclose((error) => {
      if (!isMounted) return;
      setConnectionState("disconnected");
      setLastError(error?.message ?? null);
    });

    const start = async () => {
      try
      {
        setConnectionState("connecting");
        await connection.start();
        setLastError(null);
        setConnectionState("connected");
        setMetrics((prev) => ({ ...prev, connectedAt: Date.now() }));
        await Promise.all(symbols.map((s) => connection.invoke("Subscribe", s)));
      }
      catch (err)
      {
        console.error(err);
        setLastError((err as Error).message);
        setConnectionState("disconnected");
      }
    };

    start();

    return () => {
      isMounted = false;
      if (rafRef.current !== null) cancelAnimationFrame(rafRef.current);
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, [symbols]);

  // If we've successfully connected again, clear any stale error message
  useEffect(() => {
    if (connectionState === "connected") {
      setLastError(null);
    }
  }, [connectionState]);

  return { rows, connectionState, metrics, lastError, history };
}

function App() {
  const [now, setNow] = useState(Date.now());
  const { rows, connectionState, metrics, lastError, history } = useTickerStream(DEFAULT_SYMBOLS);

  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(id);
  }, []);

  const columnDefs = useMemo<ColDef<TickerRow>[]>(
    () => [
      { field: "symbol", headerName: "Symbol", width: 120, sort: "asc" },
      {
        field: "lastPrice",
        headerName: "Last",
        valueFormatter: (p) => formatNumber(p.value),
        cellClass: (p) => (p.data?.change ?? 0) >= 0 ? "cell-up" : "cell-down",
      },
      {
        field: "bidPrice",
        headerName: "Bid",
        valueFormatter: (p) => formatNumber(p.value),
      },
      {
        field: "askPrice",
        headerName: "Ask",
        valueFormatter: (p) => formatNumber(p.value),
      },
      {
        headerName: "Spread",
        valueGetter: (p) =>
          p.data ? Math.max(0, p.data.askPrice - p.data.bidPrice) : 0,
        valueFormatter: (p) => formatNumber(p.value),
      },
      {
        headerName: "Updated",
        valueFormatter: (p) =>
          p.data ? format(p.data.tsUnixMs, "HH:mm:ss") : "",
        width: 140,
      },
    ],
    []
  );

  const connectedDuration =
    metrics.connectedAt > 0 ? Math.max(0, Math.floor((now - metrics.connectedAt) / 1000)) : 0;

  return (
    <div className="app-shell">
      <header className="hero">
        <div>
          <p className="eyebrow">MarketPulse RT</p>
          <h1>Realtime crypto tape</h1>
          <p className="lede">
            Binance feeds → gRPC → SignalR → AG Grid. Monitor latency and stability as ticks flow.
          </p>
          <div className="chip-row">
            {DEFAULT_SYMBOLS.map((s) => (
              <span key={s} className="chip">
                {s}
              </span>
            ))}
          </div>
        </div>
        <Pill
          tone={
            connectionState === "connected"
              ? "success"
              : connectionState === "reconnecting"
              ? "warning"
              : connectionState === "connecting"
              ? "neutral"
              : "danger"
          }
        >
          {connectionState === "connected" && "Live stream"}
          {connectionState === "connecting" && "Connecting"}
          {connectionState === "reconnecting" && "Reconnecting"}
          {connectionState === "disconnected" && "Offline"}
        </Pill>
      </header>

      <section className="metrics">
        <div className="metric-card">
          <p className="metric-label">Total updates</p>
          <p className="metric-value">{metrics.totalUpdates.toLocaleString()}</p>
          <p className="metric-sub">Since app load</p>
        </div>
        <div className="metric-card">
          <p className="metric-label">Updates / sec</p>
          <p className="metric-value">{metrics.updatesPerSec.toFixed(2)}</p>
          <p className="metric-sub">Rolling window per animation frame</p>
        </div>
        <div className="metric-card">
          <p className="metric-label">Connection</p>
          <p className="metric-value">
            {connectionState === "connected" ? `${connectedDuration}s` : "—"}
          </p>
          <p className="metric-sub">Connected duration</p>
        </div>
      </section>

      <section className="grid-card">
        <div className="grid-card__header">
          <div>
            <p className="eyebrow">Live stream</p>
            <h3>Tick updates</h3>
          </div>
          <div className="legend">
            <span className="legend-dot up" /> uptick
            <span className="legend-dot down" /> downtick
          </div>
        </div>
        <div className="ag-theme-quartz-dark grid">
          <AgGridReact<TickerRow>
            rowData={rows}
            columnDefs={columnDefs}
            animateRows
            domLayout="autoHeight"
            rowHeight={44}
            headerHeight={42}
            suppressCellFocus
          />
        </div>
      </section>

      <section className="sparkline-section">
        <div className="grid-card__header">
          <div>
            <p className="eyebrow">Micro trends</p>
            <h3>Uptick / downtick history</h3>
          </div>
        </div>
        <div className="sparkline-grid">
          {DEFAULT_SYMBOLS.map((s) => (
            <Card key={s} className="spark-card">
              <div className="spark-card__header">
                <span className="chip">{s}</span>
                <span className="spark-card__label">~ last 90 ticks</span>
              </div>
              <Sparkline data={history[s] ?? []} />
            </Card>
          ))}
        </div>
      </section>

      {lastError && connectionState !== "connected" && (
        <Card className="alert">
          <p className="alert-title">Connection hiccup</p>
          <p className="alert-body">{lastError}</p>
          <p className="alert-hint">
            Check that MarketDataService is on :5001 and ApiGateway on :5100. Reload to retry.
          </p>
        </Card>
      )}
    </div>
  );
}

const formatNumber = (value: number | undefined) =>
  typeof value === "number"
    ? new Intl.NumberFormat("en-US", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(value)
    : "";

type SparklinePoint = { ts: number; price: number };

const Sparkline: React.FC<{ data: SparklinePoint[] }> = ({ data }) => {
  if (!data.length) {
    return <div className="sparkline-empty">Waiting for ticks…</div>;
  }

  const prices = data.map((d) => d.price);
  const min = Math.min(...prices);
  const max = Math.max(...prices);
  const range = Math.max(max - min, 0.0001);
  const width = 240;
  const height = 60;

  const points = data.map((d, i) => {
    const x = (i / Math.max(data.length - 1, 1)) * width;
    const y = height - ((d.price - min) / range) * height;
    return `${x.toFixed(2)},${y.toFixed(2)}`;
  });

  const last = data[data.length - 1];
  const first = data[0];
  const isUp = last.price >= first.price;

  return (
    <div className="sparkline">
      <svg viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none">
        <defs>
          <linearGradient id="spark-fill" x1="0" x2="0" y1="0" y2="1">
            <stop offset="0%" stopColor={isUp ? "#56f5c5" : "#ff8c8c"} stopOpacity="0.25" />
            <stop offset="100%" stopColor={isUp ? "#56f5c5" : "#ff8c8c"} stopOpacity="0" />
          </linearGradient>
        </defs>
        <polyline
          fill="none"
          stroke={isUp ? "#56f5c5" : "#ff8c8c"}
          strokeWidth="2.2"
          points={points.join(" ")}
        />
        <polygon
          fill="url(#spark-fill)"
          points={`${points.join(" ")} ${width},${height} 0,${height}`}
        />
        <circle
          cx={points.length ? points[points.length - 1].split(",")[0] : "0"}
          cy={points.length ? points[points.length - 1].split(",")[1] : "0"}
          r="3.5"
          fill={isUp ? "#56f5c5" : "#ff8c8c"}
          stroke="rgba(0,0,0,0.3)"
          strokeWidth="1"
        />
      </svg>
      <div className="sparkline-footer">
        <span className={isUp ? "cell-up" : "cell-down"}>
          {isUp ? "▲" : "▼"} {formatNumber(last.price)}
        </span>
        <span className="sparkline-delta">
          {formatNumber(last.price - first.price)}{" "}
          {first.price !== 0
            ? `(${(((last.price - first.price) / first.price) * 100).toFixed(2)}%)`
            : ""}
        </span>
      </div>
    </div>
  );
};

export default App;
