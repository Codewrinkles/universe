import { Card } from "../../components/ui/Card";

function QuickPromptChip({ label }: { label: string }): JSX.Element {
  return (
    <button
      type="button"
      className="w-full rounded-xl border border-border bg-surface-card2 px-3 py-2 text-left text-xs text-text-secondary hover:border-brand-soft/60 hover:bg-surface-page transition-all duration-150"
    >
      {label}
    </button>
  );
}

export function SuggestedPrompts(): JSX.Element {
  return (
    <Card>
      <h2 className="text-sm font-semibold tracking-tight">Suggested prompts</h2>
      <div className="mt-3 space-y-2 text-xs">
        <QuickPromptChip label="Summarize my last week of content" />
        <QuickPromptChip label="Suggest a Learn path from my posts" />
        <QuickPromptChip label="Draft a thread from this idea" />
      </div>
    </Card>
  );
}
