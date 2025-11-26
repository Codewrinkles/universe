import type { ChatMessage } from "../../types";
import { SuggestedPrompts } from "./SuggestedPrompts";
import { ChatWindow } from "./ChatWindow";

const MOCK_MESSAGES: ChatMessage[] = [
  {
    id: 1,
    from: "system",
    text: "Tip: Ask about your own posts, videos or drafts. Twin knows your world first.",
  },
  {
    id: 2,
    from: "twin",
    text: "Hey, I'm your Codewrinkles Twin. Ask me about your content, code or training.",
  },
  {
    id: 3,
    from: "you",
    text: "Give me 3 ideas for a video that mixes .NET architecture and ultra-running.",
  },
  {
    id: 4,
    from: "twin",
    text: "Idea #1: 'Designing endurance architectures' – mapping long runs to long-lived systems...",
  },
];

export function TwinPage(): JSX.Element {
  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-surface-card1 border border-brand-soft/60">
            <span className="text-[15px] text-brand-soft">✺</span>
          </div>
          <div>
            <h1 className="text-base font-semibold tracking-tight text-text-primary">Nova</h1>
            <p className="text-xs text-text-secondary">
              Learn through conversation with AI-powered insights.
            </p>
          </div>
        </div>
        <span className="inline-flex items-center rounded-full border border-brand-soft/40 bg-brand-soft/10 px-3 py-[3px] text-[11px] font-medium text-brand-soft">
          NOVA CHAT
        </span>
      </div>

      <div className="grid gap-4 lg:grid-cols-[minmax(0,0.9fr),minmax(0,1.7fr)]">
        <SuggestedPrompts />
        <ChatWindow messages={MOCK_MESSAGES} />
      </div>
    </div>
  );
}
