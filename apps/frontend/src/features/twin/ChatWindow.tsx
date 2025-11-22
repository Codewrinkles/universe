import { useState } from "react";
import type { ChatMessage } from "../../types";
import { ChatBubble } from "./ChatBubble";
import { TypingIndicator } from "./TypingIndicator";

export interface ChatWindowProps {
  messages: ChatMessage[];
}

export function ChatWindow({ messages }: ChatWindowProps): JSX.Element {
  const [input, setInput] = useState("");

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();
    // Placeholder for future backend integration
    if (input.trim()) {
      setInput("");
    }
  };

  return (
    <div className="flex flex-col rounded-2xl border border-border bg-surface-card1">
      <div className="flex-1 space-y-3 overflow-y-auto p-4 text-xs max-h-[380px]">
        {messages.map((message) => (
          <ChatBubble key={message.id} message={message} />
        ))}
        <TypingIndicator />
      </div>
      <div className="border-t border-border-deep p-3">
        <form className="flex items-end gap-2" onSubmit={handleSubmit}>
          <textarea
            rows={2}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Ask your Twin something..."
            className="flex-1 resize-none rounded-xl border border-border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page"
          />
          <button
            type="submit"
            className="btn-primary inline-flex items-center rounded-xl bg-brand-soft px-3 py-2 text-xs font-medium text-black hover:bg-brand transition-colors"
          >
            Send
          </button>
        </form>
      </div>
    </div>
  );
}
