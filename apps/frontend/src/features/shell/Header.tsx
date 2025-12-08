import { Link, useLocation } from "react-router-dom";
import type { Theme } from "../../types";
import { MobileNav } from "./MobileNav";
import { AppSwitcher } from "./AppSwitcher";
import { ThemeToggle } from "./ThemeToggle";
import { ProfileDropdown } from "./ProfileDropdown";
import { YouTubeButton } from "../../components/ui/YouTubeButton";
import { useAuth } from "../../hooks/useAuth";

interface CurrentApp {
  name: string;
  colorClass: string;
}

function getCurrentApp(pathname: string): CurrentApp | null {
  if (pathname.startsWith("/pulse")) return { name: "Pulse", colorClass: "text-sky-400" };
  if (pathname.startsWith("/nova")) return { name: "Nova", colorClass: "text-violet-400" };
  if (pathname.startsWith("/settings")) return { name: "Settings", colorClass: "text-amber-400" };
  if (pathname.startsWith("/admin")) return { name: "Admin", colorClass: "text-emerald-400" };
  return null;
}

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

  // Current app for header display
  const currentApp = getCurrentApp(location.pathname);

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

        {/* Current app indicator - centered, visible on larger screens for authenticated users */}
        <div className="flex-1 flex justify-center">
          {isAuthenticated && currentApp && (
            <span className={`hidden sm:block text-2xl font-semibold tracking-tight ${currentApp.colorClass}`}>
              {currentApp.name}
            </span>
          )}
        </div>

        {/* Right side */}
        <div className="flex items-center gap-2 relative">
          {/* YouTube link */}
          <YouTubeButton variant="header" />

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
