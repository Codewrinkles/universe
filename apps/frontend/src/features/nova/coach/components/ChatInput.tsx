import { useState, useRef, useEffect } from "react";

interface ChatInputProps {
  onSend: (message: string) => void;
  disabled?: boolean;
  initialValue?: string;
}

/**
 * ChatInput - Input area for sending messages to Cody
 *
 * Features:
 * - Auto-expanding textarea
 * - Send button
 * - Keyboard shortcut (Cmd/Ctrl + Enter)
 * - Disabled state during streaming
 */
export function ChatInput({ onSend, disabled = false, initialValue = "" }: ChatInputProps): JSX.Element {
  const [value, setValue] = useState(initialValue);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Update value when initialValue changes (from starter cards)
  useEffect(() => {
    if (initialValue) {
      setValue(initialValue);
      textareaRef.current?.focus();
    }
  }, [initialValue]);

  // Auto-resize textarea
  useEffect(() => {
    const textarea = textareaRef.current;
    if (textarea) {
      textarea.style.height = "auto";
      textarea.style.height = `${Math.min(textarea.scrollHeight, 200)}px`;
    }
  }, [value]);

  const handleSubmit = (): void => {
    const trimmed = value.trim();
    if (trimmed && !disabled) {
      onSend(trimmed);
      setValue("");
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>): void => {
    // Cmd/Ctrl + Enter to send
    if (e.key === "Enter" && (e.metaKey || e.ctrlKey)) {
      e.preventDefault();
      handleSubmit();
    }
  };

  return (
    <div className="border-t border-border bg-surface-page p-4">
      <div className="flex items-end gap-3">
        <div className="flex-1 relative">
          <textarea
            ref={textareaRef}
            value={value}
            onChange={(e) => setValue(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Ask Cody anything..."
            disabled={disabled}
            rows={3}
            className="w-full resize-none rounded-xl border border-border bg-surface-card1 px-4 py-3 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-violet-500/50 focus:border-violet-500/50 disabled:opacity-50 disabled:cursor-not-allowed custom-scrollbar"
          />
        </div>

        <button
          type="button"
          onClick={handleSubmit}
          disabled={disabled || !value.trim()}
          className="flex items-center justify-center w-10 h-10 rounded-xl bg-violet-500 text-white hover:bg-violet-400 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex-shrink-0"
          aria-label="Send message"
        >
          <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
          </svg>
        </button>
      </div>

      {/* Hint */}
      <p className="mt-2 text-[11px] text-text-tertiary text-center">
        Press <kbd className="px-1.5 py-0.5 rounded bg-surface-card1 border border-border font-mono">⌘</kbd> + <kbd className="px-1.5 py-0.5 rounded bg-surface-card1 border border-border font-mono">Enter</kbd> to send
      </p>
    </div>
  );
}
