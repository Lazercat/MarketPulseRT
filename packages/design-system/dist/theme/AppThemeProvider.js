import { jsx as _jsx } from "react/jsx-runtime";
import { ThemeProvider } from "@emotion/react";
const theme = {
    colors: {
        primary: "#38BDF8",
    },
};
export const AppThemeProvider = ({ children }) => (_jsx(ThemeProvider, { theme: theme, children: children }));
