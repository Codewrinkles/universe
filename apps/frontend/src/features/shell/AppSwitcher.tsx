import { useState } from "react";
import { Link } from "react-router-dom";
import type { App } from "../../types";

interface AppWithPath extends App {
  path?: string;
}

const APPS: AppWithPath[] = [
  { id: "social", name: "Social", accent: "sky", description: "Micro-thought stream", path: "/social" },
  { id: "learn", name: "Learn", accent: "violet", description: "Guided learning paths", path: "/learn" },
  { id: "twin", name: "Twin", accent: "brand", description: "Your knowledge twin", path: "/twin" },
  { id: "legal", name: "Legal", accent: "amber", description: "Contracts (soon)" },
  { id: "runwrinkles", name: "Runwrinkles", accent: "emerald", description: "Running coach (soon)" },
];

function getAccentClass(accent: App["accent"]): string {
  const accentClasses = {
    sky: "bg-sky-400",
    violet: "bg-violet-400",
    brand: "bg-brand-soft",
    amber: "bg-amber-400",
    emerald: "bg-emerald-400",
  };
  return accentClasses[accent];
}

export function AppSwitcher(): JSX.Element {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="hidden sm:flex items-center gap-2 rounded-full border border-border bg-surface-card1 px-3 py-1 text-xs text-text-secondary hover:border-brand-soft/60 hover:bg-surface-card2 transition-all duration-150"
      >
        <span className="inline-flex h-4 w-4 items-center justify-center rounded-full bg-surface-page text-[10px] text-brand-soft">
          ⬢
        </span>
        <span>Apps</span>
        <span className="text-[9px] opacity-70">{isOpen ? "▲" : "▼"}</span>
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-64 rounded-2xl border border-border bg-surface-card1 shadow-sm p-3 z-20 animate-fadeIn">
          <p className="mb-2 text-[11px] text-text-tertiary">
            Switch between apps in your Codewrinkles universe.
          </p>
          <div className="space-y-1">
            {APPS.map((app) => {
              const Component = app.path ? Link : "div";
              const clickHandler = app.path ? () => setIsOpen(false) : undefined;
              return (
                <Component
                  key={app.id}
                  to={app.path || ""}
                  onClick={clickHandler}
                  className="w-full flex items-start justify-between gap-2 rounded-xl px-2.5 py-2 text-left text-xs text-text-secondary hover:bg-surface-card2 transition-colors cursor-pointer"
                >
                  <div>
                    <div className="flex items-center gap-2">
                      <span className={`h-1.5 w-1.5 rounded-full ${getAccentClass(app.accent)}`}></span>
                      <span className="text-text-primary">{app.name}</span>
                    </div>
                    <span className="mt-0.5 block text-[11px] text-text-tertiary">
                      {app.description}
                    </span>
                  </div>
                  {(app.id === "legal" || app.id === "runwrinkles") && (
                    <span className="rounded-full border border-border px-1.5 py-[1px] text-[9px] text-text-tertiary">
                      Soon
                    </span>
                  )}
                </Component>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
