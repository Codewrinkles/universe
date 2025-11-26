import { useState, useEffect } from "react";
import type { Theme } from "../types";

const THEME_STORAGE_KEY = "cw-theme";

/**
 * Read theme from localStorage synchronously.
 * Returns the stored theme or "dark" as default.
 */
function getStoredTheme(): Theme {
  try {
    const stored = window.localStorage.getItem(THEME_STORAGE_KEY);
    if (stored === "light" || stored === "dark") {
      return stored;
    }
  } catch {
    // localStorage might be unavailable (e.g., in private browsing)
  }
  return "dark";
}

/**
 * Custom hook for managing dark/light theme with localStorage persistence.
 * Applies theme class to document.documentElement for Tailwind's dark mode.
 */
export function useTheme(): {
  theme: Theme;
  toggleTheme: () => void;
  setTheme: (theme: Theme) => void;
} {
  // Initialize from localStorage to avoid race condition
  const [theme, setThemeState] = useState<Theme>(getStoredTheme);

  // Apply theme class to document and persist to localStorage
  useEffect(() => {
    try {
      window.localStorage.setItem(THEME_STORAGE_KEY, theme);
    } catch (error) {
      console.warn("Failed to save theme to localStorage:", error);
    }

    // Remove both theme classes first, then add the active one
    document.documentElement.classList.remove("light-theme", "dark-theme");
    document.documentElement.classList.add(`${theme}-theme`);
  }, [theme]);

  const toggleTheme = (): void => {
    setThemeState((prev) => (prev === "dark" ? "light" : "dark"));
  };

  const setTheme = (newTheme: Theme): void => {
    setThemeState(newTheme);
  };

  return { theme, toggleTheme, setTheme };
}
