import { Card } from "../../components/ui/Card";

export interface ComposerProps {
  value: string;
  onChange: (value: string) => void;
  maxChars: number;
  isOverLimit: boolean;
  charsLeft: number;
}

function QuickTag({ label }: { label: string }): JSX.Element {
  return (
    <button
      type="button"
      className="rounded-full border border-sky-500/40 bg-sky-900/40 px-2.5 py-[3px] text-[11px] text-sky-200 hover:border-sky-300/70 hover:bg-sky-900/60 transition-colors"
    >
      {label}
    </button>
  );
}

export function Composer({ value, onChange, maxChars, isOverLimit, charsLeft }: ComposerProps): JSX.Element {
  return (
    <Card>
      <div className="flex gap-3">
        <div className="mt-1 flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-full border border-border bg-surface-card2 text-xs font-semibold text-text-primary">
          D
        </div>
        <div className="flex-1 space-y-3">
          <textarea
            value={value}
            onChange={(e) => onChange(e.target.value)}
            rows={3}
            placeholder="What are you thinking about right now?"
            className="w-full resize-none rounded-xl border border-border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page"
          />
          <div className="flex flex-wrap items-center justify-between gap-3 text-xs">
            <div className="flex flex-wrap items-center gap-2">
              <QuickTag label="#dotnet" />
              <QuickTag label="#ai" />
              <QuickTag label="#architecture" />
            </div>
            <div className="flex items-center gap-3">
              <span
                className={`tabular-nums ${
                  isOverLimit ? "text-red-400" : "text-text-tertiary"
                }`}
              >
                {charsLeft}/{maxChars}
              </span>
              <button
                type="button"
                disabled={value.trim().length === 0 || isOverLimit}
                className={`btn-primary inline-flex items-center rounded-full px-4 py-1.5 text-xs font-medium transition-colors ${
                  value.trim().length === 0 || isOverLimit
                    ? "bg-border text-text-tertiary cursor-not-allowed"
                    : "bg-brand-soft text-black hover:bg-brand"
                }`}
              >
                Post
              </button>
            </div>
          </div>
        </div>
      </div>
    </Card>
  );
}
