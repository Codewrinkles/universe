import { NavLink } from "react-router";

interface NavItemProps {
  to: string;
  icon: string;
  label: string;
  onClick?: () => void;
}

function NavItem({ to, icon, label, onClick }: NavItemProps): JSX.Element {
  return (
    <NavLink
      to={to}
      onClick={onClick}
      className={({ isActive }) =>
        `flex items-center gap-3 rounded-full px-4 py-3 text-sm font-medium transition-colors ${
          isActive
            ? "bg-surface-card2 text-text-primary"
            : "text-text-secondary hover:bg-surface-card1 hover:text-text-primary"
        }`
      }
    >
      <span className="text-lg w-6 text-center">{icon}</span>
      <span className="md:hidden xl:inline">{label}</span>
    </NavLink>
  );
}

export interface AdminNavigationProps {
  onMobileClose: () => void;
}

export function AdminNavigation({ onMobileClose }: AdminNavigationProps): JSX.Element {
  return (
    <nav className="h-screen px-2 py-4 border-r border-border bg-surface-page">
      {/* Close button for mobile */}
      <div className="flex justify-between items-center mb-4 md:hidden">
        <span className="text-sm font-semibold text-text-primary px-4">Admin Menu</span>
        <button
          type="button"
          onClick={onMobileClose}
          className="flex items-center justify-center w-8 h-8 rounded-lg text-text-secondary hover:bg-surface-card1 transition-colors"
          aria-label="Close menu"
        >
          <span className="text-xl">✕</span>
        </button>
      </div>

      <div className="flex flex-col gap-1">
        <NavItem to="/admin" icon="📊" label="Dashboard" onClick={onMobileClose} />
        <NavItem to="/admin/users" icon="👥" label="Users" onClick={onMobileClose} />
        <NavItem to="/admin/content" icon="📝" label="Content" onClick={onMobileClose} />
        <NavItem to="/admin/settings" icon="⚙️" label="Settings" onClick={onMobileClose} />
      </div>
    </nav>
  );
}
