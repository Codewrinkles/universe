import { NavLink } from "react-router-dom";

const NAV_ITEMS = [
  { label: "Home", path: "/" },
  { label: "Pulse", path: "/pulse" },
  { label: "Nova", path: "/nova" },
];

export function AppNav(): JSX.Element {
  return (
    <nav className="hidden lg:flex gap-1 rounded-full border border-border bg-surface-card1 px-2 py-1">
      {NAV_ITEMS.map((item) => (
        <NavLink
          key={item.path}
          to={item.path}
          className={({ isActive }) =>
            `rounded-full px-4 py-1.5 text-xs transition-all duration-150 ${
              isActive
                ? "bg-brand-soft text-black font-medium shadow-sm"
                : "text-text-secondary hover:bg-surface-card2 hover:text-text-primary"
            }`
          }
        >
          {item.label}
        </NavLink>
      ))}
    </nav>
  );
}
