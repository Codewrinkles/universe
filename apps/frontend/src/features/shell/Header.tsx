import { Link, useLocation } from "react-router-dom";
import type { Theme } from "../../types";
import { AppNav } from "./AppNav";
import { MobileNav } from "./MobileNav";
import { AppSwitcher } from "./AppSwitcher";
import { ThemeToggle } from "./ThemeToggle";
import { ProfileDropdown } from "./ProfileDropdown";

export interface HeaderProps {
  theme: Theme;
  onThemeToggle: () => void;
}

export function Header({ theme, onThemeToggle }: HeaderProps): JSX.Element {
  const location = useLocation();
  const isOnboardingPage = location.pathname === "/onboarding";

  return (
    <header className="sticky top-0 z-50 border-b border-border-deep bg-surface-page/95 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 gap-3 relative">
        {/* Brand */}
        <Link to="/" className="flex items-center focus:outline-none group">
          <img
            src="/logo.png"
            alt="Codewrinkles"
            className="h-10 w-auto"
          />
        </Link>

        {/* Center nav (only when not on onboarding page) */}
        {!isOnboardingPage && <AppNav />}

        {/* Right side */}
        <div className="flex items-center gap-2 relative">
          {/* Theme toggle */}
          <ThemeToggle theme={theme} onToggle={onThemeToggle} />

          {/* Apps button + dropdown (not on onboarding page) */}
          {!isOnboardingPage && <AppSwitcher />}

          {/* Profile dropdown with avatar */}
          {!isOnboardingPage && <ProfileDropdown />}
        </div>
      </div>

      {/* Mobile nav (not on onboarding page) */}
      {!isOnboardingPage && <MobileNav />}
    </header>
  );
}
