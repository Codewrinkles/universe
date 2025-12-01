import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { HelmetProvider } from "react-helmet-async";
import { AuthProvider } from "./hooks/useAuth";
import { App } from "./App";
import "./index.css";

/**
 * Application entry point
 * Sets up providers in correct order:
 * 1. BrowserRouter (routing context)
 * 2. HelmetProvider (SEO meta tags)
 * 3. AuthProvider (authentication context)
 * 4. App (routes and UI)
 */
const rootElement = document.getElementById("root");

if (!rootElement) {
  throw new Error("Failed to find the root element");
}

ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <BrowserRouter>
      <HelmetProvider>
        <AuthProvider>
          <App />
        </AuthProvider>
      </HelmetProvider>
    </BrowserRouter>
  </React.StrictMode>
);
