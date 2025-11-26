import { createRoot } from "react-dom/client";
import App from "./App";
import { AppThemeProvider } from "@app/design-system";

createRoot(document.getElementById("root")!).render(
  <AppThemeProvider>
    <App />
  </AppThemeProvider>
);
