import { NavLink } from "react-router-dom";

const NAV_ITEMS = [
  { label: "Home", path: "/" },
  { label: "Pulse", path: "/pulse" },
  { label: "Nova", path: "/nova" },
];

export function MobileNav(): JSX.Element {
  return (
    <div className="border-t border-border-deep lg:hidden">
      <div className="mx-auto flex max-w-6xl gap-1 overflow-x-auto px-4 py-2 text-[11px]">
        {NAV_ITEMS.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            className={({ isActive }) =>
              `whitespace-nowrap rounded-full px-3 py-1 text-xs transition-all duration-150 ${
                isActive
                  ? "bg-brand-soft text-black"
                  : "border border-border bg-surface-card1 text-text-secondary hover:border-brand-soft/60 hover:text-text-primary"
              }`
            }
          >
            {item.label}
          </NavLink>
        ))}
      </div>
    </div>
  );
}
