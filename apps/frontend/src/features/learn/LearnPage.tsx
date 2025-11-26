import type { Module } from "../../types";
import { Card } from "../../components/ui/Card";
import { PathProgress } from "./PathProgress";
import { ModulesList } from "./ModulesList";

const MOCK_MODULES: Module[] = [
  { id: 1, title: "Architectural boundaries", duration: "18 min", status: "done" },
  { id: 2, title: "Vertical slices vs layers", duration: "22 min", status: "in-progress" },
  { id: 3, title: "Application services & orchestration", duration: "16 min", status: "todo" },
  { id: 4, title: "Testing boundaries", duration: "24 min", status: "todo" },
];

function SuggestedPath({ title, description }: { title: string; description: string }): JSX.Element {
  return (
    <button
      type="button"
      className="w-full rounded-xl border border-border bg-surface-card2 px-3 py-2 text-left text-xs text-text-secondary hover:border-violet-300/60 hover:bg-surface-page transition-colors"
    >
      <span className="block text-xs font-medium text-text-primary">{title}</span>
      <span className="block text-xs text-text-secondary mt-1">{description}</span>
    </button>
  );
}

export function LearnPage(): JSX.Element {
  return (
    <div className="mx-auto max-w-6xl px-4 py-6 lg:py-8 space-y-6">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-surface-card1 border border-violet-500/60">
            <span className="text-[15px] text-violet-300">▢</span>
          </div>
          <div>
            <h1 className="text-base font-semibold tracking-tight text-text-primary">Learn</h1>
            <p className="text-xs text-text-secondary">AI-guided paths based on your content.</p>
          </div>
        </div>
        <span className="inline-flex items-center rounded-full border border-violet-700 bg-violet-900/40 px-3 py-[3px] text-[11px] font-medium text-violet-200">
          LEARN DASHBOARD
        </span>
      </div>

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.6fr),minmax(0,1.1fr)]">
        <Card>
          <h2 className="text-sm font-semibold tracking-tight">Current path</h2>
          <p className="mt-1 text-xs text-text-secondary">Clean Architecture in .NET</p>
          <div className="mt-3 space-y-3 text-xs">
            <PathProgress label="Foundations & boundaries" progress={100} />
            <PathProgress label="Vertical slice features" progress={60} />
            <PathProgress label="Testing the edges" progress={20} />
          </div>
        </Card>

        <div className="space-y-3">
          <Card>
            <h3 className="text-sm font-semibold tracking-tight">Suggested next</h3>
            <div className="mt-3 space-y-2 text-xs">
              <SuggestedPath
                title="Agentic AI for existing SaaS"
                description="Turn a legacy SaaS into an AI-native system."
              />
              <SuggestedPath
                title="Ultra-running as a product metaphor"
                description="Use endurance principles to design calmer architectures."
              />
            </div>
          </Card>
          <Card>
            <h3 className="text-sm font-semibold tracking-tight">Your learning tempo</h3>
            <p className="mt-2 text-xs text-text-secondary">
              Last 7 days: <span className="text-text-primary">4</span> focused sessions ·{" "}
              <span className="text-text-primary">3</span> completed modules.
            </p>
          </Card>
        </div>
      </div>

      {/* Modules list */}
      <Card>
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-sm font-semibold tracking-tight">Modules in this path</h3>
          <span className="text-[11px] text-text-tertiary">{MOCK_MODULES.length} modules</span>
        </div>
        <ModulesList modules={MOCK_MODULES} />
      </Card>
    </div>
  );
}
