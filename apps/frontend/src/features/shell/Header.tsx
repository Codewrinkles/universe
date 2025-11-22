import { Link, useLocation } from "react-router-dom";
import type { Theme } from "../../types";
import { AppNav } from "./AppNav";
import { MobileNav } from "./MobileNav";
import { AppSwitcher } from "./AppSwitcher";
import { ThemeToggle } from "./ThemeToggle";

export interface HeaderProps {
  theme: Theme;
  onThemeToggle: () => void;
}

export function Header({ theme, onThemeToggle }: HeaderProps): JSX.Element {
  const location = useLocation();
  const isAuthPage = location.pathname === "/login" || location.pathname === "/register";
  const isOnboardingPage = location.pathname === "/onboarding";
  return (
    <header className="border-b border-border-deep bg-surface-page/95 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 gap-3 relative">
        {/* Brand */}
        <Link to="/" className="flex items-center gap-2 focus:outline-none group">
          <div className="relative flex h-10 w-10 items-center justify-center rounded-2xl bg-surface-card1 border border-brand-soft/50 shadow-sm transition-all duration-150 group-hover:border-brand-soft group-hover:shadow-md">
            <span className="text-sm font-semibold tracking-tight text-text-primary">CW</span>
            <span className="pointer-events-none absolute inset-0 rounded-2xl ring-1 ring-brand-soft/40 blur-[1px]" />
          </div>
          <div className="text-left">
            <span className="block text-base font-semibold tracking-tight text-text-primary">
              Codewrinkles
            </span>
            <span className="block text-xs text-text-secondary">
              One account. Many apps.
            </span>
          </div>
        </Link>

        {/* Center nav (only when not on auth/onboarding pages) */}
        {!isAuthPage && !isOnboardingPage && <AppNav />}

        {/* Right side */}
        <div className="flex items-center gap-2 relative">
          {/* Theme toggle */}
          <ThemeToggle theme={theme} onToggle={onThemeToggle} />

          {/* Apps button + dropdown (not on auth/onboarding pages) */}
          {!isAuthPage && !isOnboardingPage && <AppSwitcher />}

          {/* Auth / back buttons */}
          {!isAuthPage && !isOnboardingPage && (
            <>
              <Link
                to="/login"
                className="hidden sm:inline-flex items-center rounded-full border border-transparent px-3 py-1 text-xs text-text-secondary hover:border-border hover:bg-surface-card1 transition-all duration-150"
              >
                Login
              </Link>
              <Link
                to="/register"
                className="btn-primary inline-flex items-center rounded-full bg-brand-soft px-3 py-1.5 text-xs font-medium text-black hover:bg-brand"
              >
                Sign up
              </Link>
            </>
          )}

          {(isAuthPage || isOnboardingPage) && (
            <Link
              to="/"
              className="inline-flex items-center rounded-full border border-border bg-surface-card1 px-3 py-1 text-xs text-text-secondary hover:border-brand-soft/60 hover:bg-surface-card2 transition-all duration-150"
            >
              ← Back to app
            </Link>
          )}

          {/* Avatar */}
          {!isAuthPage && !isOnboardingPage && (
            <button
              type="button"
              className="flex h-9 w-9 items-center justify-center rounded-full bg-surface-card1 border border-border text-sm font-semibold text-text-primary hover:border-brand-soft/60 transition-all duration-150"
            >
              D
            </button>
          )}
        </div>
      </div>

      {/* Mobile nav (not on auth/onboarding pages) */}
      {!isAuthPage && !isOnboardingPage && <MobileNav />}
    </header>
  );
}
