import { useState } from "react";
import type { Post } from "../../types";
import { Composer } from "./Composer";
import { Feed } from "./Feed";
import { PulseSidebar } from "./PulseSidebar";

const MOCK_POSTS: Post[] = [
  {
    id: 1,
    timeAgo: "2h",
    content: "Working on the Codewrinkles multi-app shell. One account, multiple worlds.",
    type: "default",
  },
  {
    id: 2,
    timeAgo: "6h",
    content: "Micro-thought: ship the smallest slice of value, but craft the surface well.",
    type: "quote",
    quoted: {
      author: "Old notebook",
      text: "Users don't experience your architecture. They experience your surfaces.",
    },
  },
  {
    id: 3,
    timeAgo: "1d",
    content: "Thinking about how to merge running coaching and software architecture into one narrative.",
    type: "thread",
    repliesPreview: 2,
  },
  {
    id: 4,
    timeAgo: "2d",
    content: "A visual I'm using to explain vertical slices.",
    type: "image",
  },
];

export function PulsePage(): JSX.Element {
  const [feedFilter, setFeedFilter] = useState<"all" | "following" | "mine">("all");
  const [composerValue, setComposerValue] = useState("");
  const maxChars = 280;
  const charsLeft = maxChars - composerValue.length;
  const isOverLimit = charsLeft < 0;

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 border-b border-border-deep pb-4 lg:flex-row lg:items-center lg:justify-between lg:pb-5">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-surface-card1 border border-sky-500/50">
              <span className="text-[15px] text-sky-300">∞</span>
            </div>
            <div className="flex flex-col">
              <h1 className="text-base font-semibold tracking-tight text-text-primary">
                Social
              </h1>
              <p className="text-xs text-text-secondary">Your micro-thought stream.</p>
            </div>
          </div>
          <span className="inline-flex items-center rounded-full border border-sky-800 bg-sky-900/40 px-2 py-[2px] text-[11px] font-medium text-sky-200">
            SOCIAL FEED
          </span>
        </div>

        <div className="flex items-center gap-2 text-xs">
          <span className="hidden text-text-tertiary sm:inline">Filter:</span>
          <div className="inline-flex items-center gap-[2px] rounded-full border border-border bg-surface-card1 p-[3px]">
            {[
              { id: "all" as const, label: "All" },
              { id: "following" as const, label: "Following" },
              { id: "mine" as const, label: "Mine" },
            ].map((f) => {
              const isActive = feedFilter === f.id;
              return (
                <button
                  key={f.id}
                  onClick={() => setFeedFilter(f.id)}
                  className={`rounded-full px-3 py-[6px] text-xs transition-all duration-150 ${
                    isActive
                      ? "bg-surface-card2 text-text-primary border border-border"
                      : "text-text-secondary hover:bg-surface-card2 hover:text-text-primary"
                  }`}
                >
                  {f.label}
                </button>
              );
            })}
          </div>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,2fr),minmax(260px,1fr)]">
        <section className="space-y-4 lg:space-y-5">
          <Composer
            value={composerValue}
            onChange={setComposerValue}
            maxChars={maxChars}
            isOverLimit={isOverLimit}
            charsLeft={charsLeft}
          />
          <Feed posts={MOCK_POSTS} />
        </section>
        <aside className="space-y-4 lg:space-y-5">
          <PulseSidebar />
        </aside>
      </div>
    </div>
  );
}
