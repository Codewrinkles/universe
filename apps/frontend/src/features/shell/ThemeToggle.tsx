import type { Theme } from "../../types";

export interface ThemeToggleProps {
  theme: Theme;
  onToggle: () => void;
}

export function ThemeToggle({ theme, onToggle }: ThemeToggleProps): JSX.Element {
  return (
    <button
      type="button"
      onClick={onToggle}
      className="hidden sm:inline-flex items-center justify-center h-8 w-8 rounded-full border border-border bg-surface-card1 text-xs text-text-secondary hover:border-brand-soft/60 hover:bg-surface-card2 transition-all duration-150"
      title="Toggle theme"
    >
      {theme === "dark" ? "☀" : "🌙"}
    </button>
  );
}
