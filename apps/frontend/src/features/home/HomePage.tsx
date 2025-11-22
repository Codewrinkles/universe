import { Link } from "react-router-dom";
import { Card } from "../../components/ui/Card";
import { QuickPromptChip } from "./QuickPromptChip";

export function HomePage(): JSX.Element {
  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-3">
          <div className="h-8 w-8 flex items-center justify-center rounded-xl bg-surface-card2 border border-brand-soft/40">
            <span className="text-brand-soft text-base">◎</span>
          </div>
          <div>
            <h1 className="text-base font-semibold tracking-tight text-text-primary">
              Control room
            </h1>
            <p className="text-xs text-text-secondary">
              Snapshot across Social, Learn and Twin.
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <span className="hidden sm:inline-flex items-center rounded-full border border-brand-soft/40 bg-brand-soft/10 px-3 py-[3px] text-[11px] font-medium text-brand-soft">
            LANDING
          </span>
          <Link
            to="/onboarding"
            className="btn-primary inline-flex items-center rounded-full border border-border bg-surface-card1 px-3 py-1.5 text-[11px] text-text-secondary hover:border-brand-soft/60 hover:bg-surface-card2"
          >
            <span className="mr-1">◎</span> Start onboarding
          </Link>
        </div>
      </div>

      <div className="grid gap-5 lg:grid-cols-3">
        {/* Social snapshot */}
        <Card>
          <div className="flex items-center justify-between gap-2">
            <h2 className="text-sm font-semibold tracking-tight">Social snapshot</h2>
            <span className="rounded-full border border-sky-800 bg-sky-900/30 px-2 py-[2px] text-[11px] text-sky-200">
              SOCIAL
            </span>
          </div>
          <p className="mt-2 text-xs text-text-secondary">
            Last 3 posts you shared.
          </p>
          <ul className="mt-3 space-y-2 text-xs text-text-secondary">
            <li className="rounded-lg bg-surface-card2 px-3 py-2">
              • Shaping the Codewrinkles multi-app shell.
            </li>
            <li className="rounded-lg bg-surface-card2 px-3 py-2">
              • Thoughts on agentic AI in real products.
            </li>
            <li className="rounded-lg bg-surface-card2 px-3 py-2">
              • Ultra-running, architecture and focus.
            </li>
          </ul>
        </Card>

        {/* Learn snapshot */}
        <Card>
          <div className="flex items-center justify-between gap-2">
            <h2 className="text-sm font-semibold tracking-tight">Learn progress</h2>
            <span className="rounded-full border border-violet-800 bg-violet-900/30 px-2 py-[2px] text-[11px] text-violet-200">
              LEARN
            </span>
          </div>
          <p className="mt-2 text-xs text-text-secondary">
            Active path:{" "}
            <span className="text-text-primary">Clean Architecture in .NET</span>
          </p>
          <div className="mt-3">
            <div className="flex items-center justify-between text-xs text-text-tertiary">
              <span>Overall</span>
              <span className="text-text-secondary">42%</span>
            </div>
            <div className="mt-1 h-2 rounded-full bg-surface-card2">
              <div className="h-2 w-[42%] rounded-full bg-violet-400"></div>
            </div>
          </div>
          <ul className="mt-3 text-xs text-text-secondary space-y-1">
            <li>• Next: Vertical slices & feature folders</li>
            <li>• Upcoming: Testing strategy for clean boundaries</li>
          </ul>
        </Card>

        {/* Twin snapshot */}
        <Card>
          <div className="flex items-center justify-between gap-2">
            <h2 className="text-sm font-semibold tracking-tight">Twin quick prompts</h2>
            <span className="rounded-full border border-brand-soft/40 bg-brand-soft/10 px-2 py-[2px] text-[11px] text-brand-soft">
              TWIN
            </span>
          </div>
          <p className="mt-2 text-xs text-text-secondary">
            Ask your Twin about your own content.
          </p>
          <div className="mt-3 space-y-2 text-xs">
            <QuickPromptChip label="Summarize my latest Social posts" />
            <QuickPromptChip label="Generate a Learn path from my backlog" />
            <QuickPromptChip label="Draft a thread about agentic AI" />
          </div>
        </Card>
      </div>
    </div>
  );
}
