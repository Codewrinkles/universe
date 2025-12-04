import { Outlet, useLocation, useNavigate } from "react-router-dom";
import type { SettingsSection } from "../../types";
import { SettingsSidebar } from "./SettingsSidebar";

const SECTIONS: SettingsSection[] = [
  { id: "profile", label: "Profile" },
  { id: "account", label: "Account" },
];

export function SettingsPage(): JSX.Element {
  const location = useLocation();
  const navigate = useNavigate();

  // Extract the active section from the URL path
  const pathSegments = location.pathname.split("/");
  const activeSection = (pathSegments[2] || "profile") as SettingsSection["id"];

  const handleMobileChange = (value: string): void => {
    navigate(`/settings/${value}`);
  };

  return (
    <div className="mx-auto max-w-6xl px-4 py-6 lg:py-8 space-y-6">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-surface-card1 border border-border">
            <span className="text-[15px] text-text-secondary">⚙</span>
          </div>
          <div>
            <h1 className="text-base font-semibold tracking-tight text-text-primary">Settings</h1>
            <p className="text-xs text-text-secondary">
              Configure your account and app behavior.
            </p>
          </div>
        </div>
        <span className="inline-flex items-center rounded-full border border-border bg-surface-card1 px-3 py-[3px] text-[11px] font-medium text-text-secondary">
          SETTINGS
        </span>
      </div>

      {/* Mobile section select */}
      <div className="lg:hidden">
        <label className="mb-1 block text-xs text-text-tertiary">Section</label>
        <select
          value={activeSection}
          onChange={(e) => handleMobileChange(e.target.value)}
          className="w-full rounded-xl border border-border bg-surface-card1 px-3 py-2 text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page"
        >
          {SECTIONS.map((section) => (
            <option key={section.id} value={section.id}>
              {section.label}
            </option>
          ))}
        </select>
      </div>

      <div className="grid gap-4 lg:grid-cols-[220px,minmax(0,1fr)]">
        {/* Sidebar */}
        <SettingsSidebar sections={SECTIONS} />

        {/* Content */}
        <section className="rounded-2xl border border-border bg-surface-card1 p-4 text-sm">
          <Outlet />
        </section>
      </div>
    </div>
  );
}
