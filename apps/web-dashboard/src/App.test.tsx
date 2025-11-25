import { describe, expect, it } from "vitest";
import { render, screen, within } from "@testing-library/react";
import App from "./App";

describe("App shell", () => {
  it("renders headline and metrics", () => {
    render(<App />);

    expect(screen.getByText(/Realtime crypto tape/i)).toBeInTheDocument();
    expect(screen.getByText(/Total updates/i)).toBeInTheDocument();
    expect(screen.getByText(/Updates \/ sec/i)).toBeInTheDocument();
  });

  it("shows default symbols as chips", () => {
    render(<App />);
    const chipRow = screen.getAllByText(/USDT/);
    expect(chipRow.length).toBeGreaterThanOrEqual(3);
  });

  it("shows live stream header", () => {
    render(<App />);
    expect(
      within(screen.getByRole("heading", { name: /Tick updates/i })).getByText(/Tick updates/i)
    ).toBeInTheDocument();
  });
});
