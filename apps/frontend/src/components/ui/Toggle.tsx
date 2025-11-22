export interface ToggleProps {
  enabled?: boolean;
  onChange?: (enabled: boolean) => void;
  label?: string;
}

export function Toggle({ enabled = false, onChange, label }: ToggleProps): JSX.Element {
  const handleClick = (): void => {
    if (onChange) {
      onChange(!enabled);
    }
  };

  return (
    <div className="flex items-center justify-between gap-3">
      {label && <span className="text-xs text-text-secondary">{label}</span>}
      <button
        type="button"
        onClick={handleClick}
        className={`relative inline-flex h-5 w-9 items-center rounded-full border transition-colors ${
          enabled
            ? "border-brand-soft/40 bg-brand-soft/20"
            : "border-border bg-surface-card2"
        }`}
      >
        <span
          className={`inline-block h-4 w-4 rounded-full transition-transform ${
            enabled
              ? "translate-x-[18px] bg-brand-soft"
              : "translate-x-[2px] bg-text-tertiary"
          }`}
        />
      </button>
    </div>
  );
}
