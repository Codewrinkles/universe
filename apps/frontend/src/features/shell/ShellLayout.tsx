import { Outlet } from "react-router-dom";
import type { Theme } from "../../types";
import { Header } from "./Header";

export interface ShellLayoutProps {
  theme: Theme;
  onThemeToggle: () => void;
}

/**
 * ShellLayout wraps the entire app with header and main content area.
 * Uses React Router's Outlet for nested route rendering.
 */
export function ShellLayout({ theme, onThemeToggle }: ShellLayoutProps): JSX.Element {
  return (
    <div className={`${theme}-theme min-h-screen bg-surface-page`}>
      <Header theme={theme} onThemeToggle={onThemeToggle} />
      <main className="mx-auto max-w-6xl px-4 py-6 lg:py-8 text-sm">
        <Outlet />
      </main>
    </div>
  );
}
