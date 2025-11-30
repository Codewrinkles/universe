import { Link, useLocation } from "react-router-dom";
import type { Theme } from "../../types";
import { MobileNav } from "./MobileNav";
import { AppSwitcher } from "./AppSwitcher";
import { ThemeToggle } from "./ThemeToggle";
import { ProfileDropdown } from "./ProfileDropdown";
import { useAuth } from "../../hooks/useAuth";

export interface HeaderProps {
  theme: Theme;
  onThemeToggle: () => void;
}

export function Header({ theme, onThemeToggle }: HeaderProps): JSX.Element {
  const location = useLocation();
  const { isAuthenticated } = useAuth();
  const isOnboardingPage = location.pathname === "/onboarding";
  const isLandingPage = location.pathname === "/";

  // Logo destination: /pulse for authenticated, / for unauthenticated
  const logoDestination = isAuthenticated ? "/pulse" : "/";

  return (
    <header className="sticky top-0 z-50 border-b border-border-deep bg-surface-page/95 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 gap-3 relative">
        {/* Brand */}
        <Link to={logoDestination} className="flex items-center focus:outline-none group">
          <img
            src="/logo.png"
            alt="Codewrinkles"
            className="h-10 w-auto"
          />
        </Link>

        {/* Center spacer - AppNav hidden for MVP */}
        <div className="flex-1" />

        {/* Right side */}
        <div className="flex items-center gap-2 relative">
          {/* Theme toggle */}
          <ThemeToggle theme={theme} onToggle={onThemeToggle} />

          {/* Unauthenticated users on landing page: Show Register/Login */}
          {!isAuthenticated && isLandingPage && (
            <>
              <Link
                to="/register"
                className="hidden sm:inline-flex items-center justify-center rounded-full bg-brand text-black px-4 py-1.5 text-xs font-medium hover:bg-brand-soft transition-colors"
              >
                Join
              </Link>
              <Link
                to="/login"
                className="hidden sm:inline-flex items-center justify-center rounded-full border border-border bg-surface-card1 text-text-secondary px-4 py-1.5 text-xs hover:border-brand-soft/60 hover:bg-surface-card2 transition-colors"
              >
                Sign In
              </Link>
            </>
          )}

          {/* Authenticated users: Show app switcher and profile */}
          {isAuthenticated && !isOnboardingPage && (
            <>
              <AppSwitcher />
              <ProfileDropdown />
            </>
          )}
        </div>
      </div>

      {/* Mobile nav (only for authenticated users, not on onboarding page) */}
      {isAuthenticated && !isOnboardingPage && <MobileNav />}
    </header>
  );
}
