import { useMemo } from "react";
import { ModuleRegistry, AllCommunityModule, ColDef } from "ag-grid-community";
import { AgGridReact } from "ag-grid-react";
import "ag-grid-community/styles/ag-theme-quartz.css";

// Register grid modules in the lazy chunk so the main bundle stays lean.
ModuleRegistry.registerModules([AllCommunityModule]);

type TickerRow = {
  symbol: string;
  lastPrice: number;
  bidPrice: number;
  askPrice: number;
  tsUnixMs: number;
  change: number;
};

type Props = {
  rows: TickerRow[];
};

const formatNumber = (value: number | undefined) =>
  typeof value === "number"
    ? new Intl.NumberFormat("en-US", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(value)
    : "";

const formatTime = (value: number | undefined) =>
  typeof value === "number"
    ? new Intl.DateTimeFormat("en-US", {
        hour12: false,
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
      }).format(value)
    : "";

const TickerGrid = ({ rows }: Props) => {
  const columnDefs = useMemo<ColDef<TickerRow>[]>(() => {
    const numericCellClass = (params: { data?: TickerRow }) => {
      const delta = params.data?.change ?? 0;
      return delta > 0 ? "cell-up" : delta < 0 ? "cell-down" : "";
    };

    return [
      {
        field: "symbol",
        headerName: "Symbol",
        minWidth: 110,
        flex: 1,
        sort: "asc",
      },
      {
        field: "lastPrice",
        headerName: "Last",
        valueFormatter: (params) => formatNumber(params.value),
        cellClass: numericCellClass,
        minWidth: 120,
        flex: 1,
      },
      {
        field: "bidPrice",
        headerName: "Bid",
        valueFormatter: (params) => formatNumber(params.value),
        cellClass: numericCellClass,
        minWidth: 120,
        flex: 1,
      },
      {
        field: "askPrice",
        headerName: "Ask",
        valueFormatter: (params) => formatNumber(params.value),
        cellClass: numericCellClass,
        minWidth: 120,
        flex: 1,
      },
      {
        headerName: "Spread",
        valueGetter: (params) => {
          const ask = params.data?.askPrice ?? 0;
          const bid = params.data?.bidPrice ?? 0;
          return Math.max(ask - bid, 0);
        },
        valueFormatter: (params) => formatNumber(params.value),
        cellClass: numericCellClass,
        minWidth: 120,
        flex: 1,
      },
      {
        field: "tsUnixMs",
        headerName: "Updated",
        valueFormatter: (params) => formatTime(params.value),
        minWidth: 140,
        flex: 1,
      },
    ];
  }, []);

  return (
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
  );
};

export default TickerGrid;
