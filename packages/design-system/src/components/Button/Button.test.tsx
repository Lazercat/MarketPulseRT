import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { Button } from "./Button";
import { ThemeProvider } from "@emotion/react";

const theme = { colors: { primary: "#000" } };

describe("Button", () => {
  it("renders children", () => {
    render(
      <ThemeProvider theme={theme}>
        <Button>Click me</Button>
      </ThemeProvider>
    );
    expect(screen.getByRole("button", { name: /click me/i })).toBeInTheDocument();
  });
});
