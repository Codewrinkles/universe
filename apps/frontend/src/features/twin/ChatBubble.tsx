import type { ChatMessage } from "../../types";

export interface ChatBubbleProps {
  message: ChatMessage;
}

export function ChatBubble({ message }: ChatBubbleProps): JSX.Element {
  if (message.from === "system") {
    return (
      <div className="flex justify-center">
        <div className="max-w-[90%] rounded-full border border-border bg-surface-page px-3 py-1.5 text-[11px] text-text-tertiary">
          {message.text}
        </div>
      </div>
    );
  }

  const isTwin = message.from === "twin";

  return (
    <div className={`flex ${isTwin ? "justify-start" : "justify-end"}`}>
      <div
        className={`max-w-[80%] rounded-2xl px-3 py-2 text-xs leading-relaxed ${
          isTwin
            ? "bg-surface-card2 border border-brand-soft/50 text-text-primary"
            : "bg-surface-page border border-border text-text-primary"
        }`}
      >
        <div className="mb-1 text-[10px] uppercase tracking-wide text-text-tertiary">
          {isTwin ? "Twin" : "You"}
        </div>
        <div>{message.text}</div>
      </div>
    </div>
  );
}
