import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { AuthProvider } from "./hooks/useAuth";
import { App } from "./App";
import "./index.css";

/**
 * Application entry point
 * Sets up providers in correct order:
 * 1. BrowserRouter (routing context)
 * 2. AuthProvider (authentication context)
 * 3. App (routes and UI)
 */
const rootElement = document.getElementById("root");

if (!rootElement) {
  throw new Error("Failed to find the root element");
}

ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
