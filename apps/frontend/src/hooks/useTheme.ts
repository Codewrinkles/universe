import { useState, useEffect } from "react";
import type { Theme } from "../types";

const THEME_STORAGE_KEY = "cw-theme";

/**
 * Custom hook for managing dark/light theme with localStorage persistence.
 * Applies theme class to document.documentElement for Tailwind's dark mode.
 */
export function useTheme(): {
  theme: Theme;
  toggleTheme: () => void;
  setTheme: (theme: Theme) => void;
} {
  const [theme, setThemeState] = useState<Theme>("dark");

  // Load theme from localStorage on mount
  useEffect(() => {
    try {
      const stored = window.localStorage.getItem(THEME_STORAGE_KEY);
      if (stored === "light" || stored === "dark") {
        setThemeState(stored);
      }
    } catch (error) {
      // localStorage might be unavailable (e.g., in private browsing)
      console.warn("Failed to read theme from localStorage:", error);
    }
  }, []);

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
