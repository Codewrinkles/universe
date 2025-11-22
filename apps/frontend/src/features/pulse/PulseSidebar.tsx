import { Card } from "../../components/ui/Card";

function ContextChip({ label }: { label: string }): JSX.Element {
  return (
    <div className="flex items-center justify-between rounded-xl border border-border bg-surface-page px-2 py-1 text-xs text-text-secondary">
      <span className="truncate">{label}</span>
      <span className="text-[9px] text-text-tertiary">●</span>
    </div>
  );
}

function TagPill({ label }: { label: string }): JSX.Element {
  return (
    <span className="inline-flex items-center rounded-full border border-border bg-surface-page px-2 py-[3px] text-[11px] text-text-secondary">
      {label}
    </span>
  );
}

export function PulseSidebar(): JSX.Element {
  return (
    <div className="space-y-3">
      <Card>
        <h2 className="text-sm font-semibold tracking-tight text-text-primary">
          Today&apos;s focus
        </h2>
        <p className="mt-1 text-xs text-text-secondary">
          Shape your stream with small, honest updates about what you&apos;re really working on.
        </p>
        <div className="mt-3 grid grid-cols-2 gap-2 text-xs">
          <ContextChip label="Architecture notes" />
          <ContextChip label="Running insights" />
          <ContextChip label="AI experiments" />
          <ContextChip label=".NET patterns" />
        </div>
      </Card>
      <Card>
        <h3 className="text-sm font-semibold tracking-tight">Pinned tags</h3>
        <div className="mt-2 flex flex-wrap gap-1.5 text-xs">
          <TagPill label="#ultra" />
          <TagPill label="#cleanarchitecture" />
          <TagPill label="#mediatr" />
          <TagPill label="#agenticAI" />
        </div>
      </Card>
      <Card>
        <h3 className="text-sm font-semibold tracking-tight">Drafts</h3>
        <p className="mt-2 text-xs text-text-tertiary">
          No drafts yet. Anything you start writing and close will show up here.
        </p>
      </Card>
    </div>
  );
}
