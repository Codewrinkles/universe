import { NavLink } from "react-router-dom";
import type { SettingsSection } from "../../types";

export interface SettingsSidebarProps {
  sections: SettingsSection[];
}

export function SettingsSidebar({ sections }: SettingsSidebarProps): JSX.Element {
  return (
    <aside className="hidden flex-col gap-1 rounded-2xl border border-border bg-surface-card1 p-2 text-xs lg:flex">
      {sections.map((section) => (
        <NavLink
          key={section.id}
          to={`/settings/${section.id}`}
          className={({ isActive }) =>
            `flex items-center justify-between rounded-xl px-3 py-2 text-left transition-colors ${
              isActive
                ? "bg-surface-card2 text-text-primary border border-border"
                : "text-text-secondary hover:bg-surface-card2"
            }`
          }
        >
          {({ isActive }) => (
            <>
              <span>{section.label}</span>
              {isActive && <span className="text-[9px] text-brand-soft">●</span>}
            </>
          )}
        </NavLink>
      ))}
    </aside>
  );
}
