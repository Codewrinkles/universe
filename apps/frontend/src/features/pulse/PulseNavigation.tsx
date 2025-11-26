import { NavLink } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";
import { config } from "../../config";

interface NavItemProps {
  to: string;
  icon: string;
  label: string;
  badge?: number;
}

function NavItem({ to, icon, label, badge }: NavItemProps): JSX.Element {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        `flex items-center gap-3 rounded-full px-4 py-3 text-sm font-medium transition-colors ${
          isActive
            ? "bg-surface-card2 text-text-primary"
            : "text-text-secondary hover:bg-surface-card1 hover:text-text-primary"
        }`
      }
    >
      <span className="text-lg w-6 text-center">{icon}</span>
      <span className="hidden xl:inline">{label}</span>
      {badge !== undefined && badge > 0 && (
        <span className="ml-auto hidden xl:inline rounded-full bg-sky-500 px-2 py-0.5 text-[10px] font-semibold text-white">
          {badge > 99 ? "99+" : badge}
        </span>
      )}
    </NavLink>
  );
}

export function PulseNavigation(): JSX.Element {
  const { user } = useAuth();

  const avatarUrl = user?.avatarUrl
    ? `${config.api.baseUrl}${user.avatarUrl}?t=${Date.now()}`
    : null;

  return (
    <nav className="sticky top-20 z-10 space-y-1">
      <NavItem to="/pulse" icon="🏠" label="Home" />
      {/* <NavItem to="/pulse/explore" icon="🔍" label="Explore" /> */}
      <NavItem to="/pulse/notifications" icon="🔔" label="Notifications" badge={3} />
      <NavItem to="/pulse/messages" icon="✉️" label="Messages" />
      <NavItem to="/pulse/bookmarks" icon="🔖" label="Bookmarks" />
      <NavItem to="/pulse/profile" icon="👤" label="Profile" />

      <div className="pt-4">
        <button
          type="button"
          className="w-full rounded-full bg-brand-soft px-4 py-3 text-sm font-semibold text-black hover:bg-brand transition-colors"
        >
          <span className="hidden xl:inline">Post</span>
          <span className="xl:hidden text-lg">✏️</span>
        </button>
      </div>

      {/* User profile card at bottom */}
      <div className="pt-6">
        <div className="flex items-center gap-3 rounded-full px-3 py-2 hover:bg-surface-card1 transition-colors cursor-pointer">
          {avatarUrl ? (
            <img
              src={avatarUrl}
              alt={user?.name ?? "Profile"}
              className="h-10 w-10 rounded-full object-cover"
            />
          ) : (
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-surface-card2 border border-border text-sm font-semibold text-text-primary">
              {user?.name?.charAt(0).toUpperCase() ?? "?"}
            </div>
          )}
          <div className="hidden xl:flex flex-col min-w-0">
            <span className="text-sm font-medium text-text-primary truncate">
              {user?.name ?? "Guest"}
            </span>
            <span className="text-xs text-text-tertiary truncate">
              @{user?.handle ?? "guest"}
            </span>
          </div>
          <span className="hidden xl:inline ml-auto text-text-tertiary">⋯</span>
        </div>
      </div>
    </nav>
  );
}
