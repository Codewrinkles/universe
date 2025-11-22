export interface QuickPromptChipProps {
  label: string;
}

export function QuickPromptChip({ label }: QuickPromptChipProps): JSX.Element {
  return (
    <button
      type="button"
      className="w-full rounded-xl border border-border bg-surface-card2 px-3 py-2 text-left text-xs text-text-secondary hover:border-brand-soft/60 hover:bg-surface-page transition-all duration-150"
    >
      {label}
    </button>
  );
}
