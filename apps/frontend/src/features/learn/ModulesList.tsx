import type { Module } from "../../types";

export interface ModulesListProps {
  modules: Module[];
}

function ModuleStatus({ status }: { status: Module["status"] }): JSX.Element {
  const statusConfig = {
    done: {
      label: "Done",
      className: "bg-emerald-900/40 text-emerald-300 border-emerald-700",
    },
    "in-progress": {
      label: "In progress",
      className: "bg-violet-900/40 text-violet-200 border-violet-700",
    },
    todo: {
      label: "Next",
      className: "bg-slate-900/40 text-text-secondary border-border",
    },
  };

  const config = statusConfig[status];

  return (
    <span className={`rounded-full border px-2 py-[2px] text-[11px] ${config.className}`}>
      {config.label}
    </span>
  );
}

export function ModulesList({ modules }: ModulesListProps): JSX.Element {
  return (
    <div className="space-y-2 text-xs">
      {modules.map((module) => (
        <div
          key={module.id}
          className="flex items-center justify-between rounded-xl border border-border bg-surface-card2 px-3 py-2 hover:border-violet-300/60 hover:bg-surface-page transition-colors"
        >
          <div className="flex flex-col">
            <span className="text-text-primary">{module.title}</span>
            <span className="text-[11px] text-text-tertiary">{module.duration}</span>
          </div>
          <ModuleStatus status={module.status} />
        </div>
      ))}
    </div>
  );
}
