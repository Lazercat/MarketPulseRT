import React from "react";
import { ThemeProvider } from "@emotion/react";
import { Theme } from "./types";

const theme: Theme = {
  colors: {
    primary: "#38BDF8",
  },
};

type Props = {
  children: React.ReactNode;
};

export const AppThemeProvider: React.FC<Props> = ({ children }) => (
  <ThemeProvider theme={theme}>{children}</ThemeProvider>
);
