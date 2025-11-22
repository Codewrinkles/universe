import { Toggle } from "../../components/ui/Toggle";

function Field({ label, placeholder }: { label: string; placeholder: string }): JSX.Element {
  return (
    <div className="space-y-1">
      <label className="block text-xs text-text-tertiary">{label}</label>
      <input
        placeholder={placeholder}
        className="w-full rounded-xl border border-border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page"
      />
    </div>
  );
}

export function SettingsProfile(): JSX.Element {
  return (
    <div className="space-y-4">
      <h2 className="text-sm font-semibold tracking-tight text-text-primary">Profile details</h2>
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Display name" placeholder="Daniel @ Codewrinkles" />
        <Field label="Handle" placeholder="@codewrinkles" />
        <Field label="Tagline" placeholder="Running, .NET and agentic AI." />
      </div>
    </div>
  );
}

export function SettingsAccount(): JSX.Element {
  return (
    <div className="space-y-4">
      <h2 className="text-sm font-semibold tracking-tight text-text-primary">
        Account & security
      </h2>
      <Field label="Email" placeholder="you@example.com" />
      <p className="text-xs text-text-tertiary">
        Authentication and advanced security settings will live here.
      </p>
    </div>
  );
}

export function SettingsApps(): JSX.Element {
  return (
    <div className="space-y-4">
      <h2 className="text-sm font-semibold tracking-tight text-text-primary">Connected apps</h2>
      <ul className="space-y-2 text-xs">
        <li className="flex items-center justify-between rounded-xl border border-border bg-surface-card2 px-3 py-2">
          <span>Social</span>
          <span className="rounded-full bg-sky-900/40 px-2 py-[2px] text-[11px] text-sky-200 border border-sky-700">
            Active
          </span>
        </li>
        <li className="flex items-center justify-between rounded-xl border border-border bg-surface-card2 px-3 py-2">
          <span>Learn</span>
          <span className="rounded-full bg-violet-900/40 px-2 py-[2px] text-[11px] text-violet-200 border border-violet-700">
            Active
          </span>
        </li>
        <li className="flex items-center justify-between rounded-xl border border-border bg-surface-card2 px-3 py-2">
          <span>Twin</span>
          <span className="rounded-full bg-brand-soft/10 px-2 py-[2px] text-[11px] text-brand-soft border border-brand-soft/40">
            Active
          </span>
        </li>
        <li className="flex items-center justify-between rounded-xl border border-border bg-surface-card2 px-3 py-2">
          <span>Legal</span>
          <span className="rounded-full bg-amber-900/30 px-2 py-[2px] text-[11px] text-amber-200 border border-amber-700">
            Coming soon
          </span>
        </li>
      </ul>
    </div>
  );
}

export function SettingsNotifications(): JSX.Element {
  return (
    <div className="space-y-4">
      <h2 className="text-sm font-semibold tracking-tight text-text-primary">Notifications</h2>
      <Toggle label="Email me weekly summaries" enabled={true} />
      <Toggle label="Notify me when Learn unlocks a new module" enabled={true} />
      <Toggle label="Twin recap of my week" enabled={true} />
    </div>
  );
}
