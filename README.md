# MarketPulse RT – Realtime Trading Dashboard (React + .NET 10)

## Goal

Build a production-grade, portfolio-ready real-time trading-style dashboard that showcases:

- Advanced **C# / .NET 10** skills (Web API, worker service, gRPC, SignalR)
- **React + TypeScript** front-end architecture with a **custom design system**
- High-frequency real-time data handling using **SignalR**, **WebSockets**, and **gRPC**
- Performance-focused **data grid** implementation (AG Grid) with frequent updates
- A **polyglot monorepo** layout (Turborepo + Yarn 4 workspaces + .NET solution)
- Containerization with **Docker** and cloud-readiness (Terraform structure for AWS)

## Concept

**MarketPulse RT** is a real-time crypto market operations dashboard:

- A .NET 10 **MarketDataService** connects to public crypto WebSocket feeds (e.g., Binance),
  normalizes data into strongly typed messages, and exposes a **gRPC streaming API**.
- A .NET 10 **ApiGateway** consumes that gRPC stream and fans out updates to clients via
  **SignalR hubs**, also exposing REST endpoints for snapshots and reference data.
- A React/TypeScript **Web Dashboard** displays streaming prices, order book data, and trade
  events in **AG Grid**, with a **custom design-system** that is optimized for performance:
  CSS-in-JS for shell + theming, static CSS for hot paths (grid cells).

## High-level Architecture

- **Frontend**

  - `apps/web-dashboard`: Vite + React + TS app
  - `packages/design-system`: Emotion + Styled System-based design system
  - `packages/web-state`: shared SignalR client, state management (Zustand), hooks
  - `packages/shared-contracts`: TS types generated from proto/OpenAPI

- **Backend (.NET 10)**

  - `apps/ApiGateway`: ASP.NET Core Web API + SignalR + gRPC client to MarketDataService
  - `apps/MarketDataService`: .NET worker using WebSockets to ingest public market data,
    exposing a gRPC streaming service

- **Infra**
  - Dockerfiles for each app
  - `docker-compose.yml` for local dev
  - `infra/terraform` skeleton for AWS ECS Fargate + RDS + networking

## Key Tech Choices

- **.NET 10** (Web API, Worker, gRPC, SignalR)
- **React 18**, **TypeScript 5**, **Vite**
- **AG Grid** for high-performance, real-time tabular data
- **Yarn 4 + Turborepo** for monorepo orchestration
- **Emotion + Styled System** for design system tokens/composability
- **Zustand** (or Redux Toolkit) for normalized client-side state
- **Docker** for containerization, **Terraform** for AWS-ready infra skeleton
