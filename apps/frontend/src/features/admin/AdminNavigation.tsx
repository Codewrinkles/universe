import { useState } from "react";
import { NavLink, useLocation } from "react-router";

interface NavItemProps {
  to: string;
  icon: string;
  label: string;
  onClick?: () => void;
  isChild?: boolean;
}

function NavItem({ to, icon, label, onClick, isChild = false }: NavItemProps): JSX.Element {
  return (
    <NavLink
      to={to}
      end={to === "/admin"}
      onClick={onClick}
      className={({ isActive }) =>
        `flex items-center gap-3 rounded-full px-4 py-3 text-sm font-medium transition-colors ${
          isChild ? "pl-10" : ""
        } ${
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

interface DropdownNavItemProps {
  icon: string;
  label: string;
  isExpanded: boolean;
  onToggle: () => void;
  isActive: boolean;
}

function DropdownNavItem({ icon, label, isExpanded, onToggle, isActive }: DropdownNavItemProps): JSX.Element {
  return (
    <button
      type="button"
      onClick={onToggle}
      className={`flex items-center gap-3 rounded-full px-4 py-3 text-sm font-medium transition-colors w-full ${
        isActive
          ? "bg-surface-card2 text-text-primary"
          : "text-text-secondary hover:bg-surface-card1 hover:text-text-primary"
      }`}
    >
      <span className="text-lg w-6 text-center">{icon}</span>
      <span className="md:hidden xl:inline flex-1 text-left">{label}</span>
      <span className={`text-xs transition-transform duration-200 ${isExpanded ? "rotate-180" : ""}`}>
        ▼
      </span>
    </button>
  );
}

export interface AdminNavigationProps {
  onMobileClose: () => void;
}

export function AdminNavigation({ onMobileClose }: AdminNavigationProps): JSX.Element {
  const location = useLocation();
  const isNovaRoute = location.pathname.startsWith("/admin/nova");
  const [novaExpanded, setNovaExpanded] = useState(isNovaRoute);

  const handleNovaToggle = (): void => {
    setNovaExpanded(!novaExpanded);
  };

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

        {/* Nova dropdown */}
        <div>
          <DropdownNavItem
            icon="🤖"
            label="Nova"
            isExpanded={novaExpanded}
            onToggle={handleNovaToggle}
            isActive={isNovaRoute}
          />
          {novaExpanded && (
            <div className="flex flex-col gap-1 mt-1">
              <NavItem
                to="/admin/nova/submissions"
                icon="📋"
                label="Submissions"
                onClick={onMobileClose}
                isChild
              />
              <NavItem
                to="/admin/nova/metrics"
                icon="📈"
                label="Metrics"
                onClick={onMobileClose}
                isChild
              />
              <NavItem
                to="/admin/nova/content"
                icon="📚"
                label="Content"
                onClick={onMobileClose}
                isChild
              />
            </div>
          )}
        </div>

        <NavItem to="/admin/users" icon="👥" label="Users" onClick={onMobileClose} />
        <NavItem to="/admin/settings" icon="⚙️" label="Settings" onClick={onMobileClose} />
      </div>
    </nav>
  );
}
