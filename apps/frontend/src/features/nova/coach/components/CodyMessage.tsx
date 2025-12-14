import type { Message } from "../../types";

interface CodyMessageProps {
  message: Message;
}

/**
 * CodyMessage - Cody's message bubble
 *
 * Features:
 * - Cody avatar and name
 * - Message content with markdown support (future)
 * - Copy button
 * - Relative timestamp
 */
export function CodyMessage({ message }: CodyMessageProps): JSX.Element {
  const handleCopy = (): void => {
    void navigator.clipboard.writeText(message.content);
  };

  // Format relative time
  const formatTime = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return `${diffMins} min ago`;

    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h ago`;

    return date.toLocaleDateString();
  };

  return (
    <div className="flex items-start gap-3 px-4 py-3 group">
      {/* Cody Avatar */}
      <div className="w-8 h-8 rounded-xl bg-violet-500/20 border border-violet-500/40 flex items-center justify-center flex-shrink-0">
        <span className="text-sm">🤖</span>
      </div>

      {/* Message Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 mb-1">
          <span className="text-xs font-semibold text-violet-400 uppercase tracking-wide">
            Cody
          </span>
        </div>

        <div className="text-sm text-text-primary leading-relaxed whitespace-pre-wrap">
          {message.content}
        </div>

        {/* Footer with actions */}
        <div className="flex items-center gap-3 mt-2 opacity-0 group-hover:opacity-100 transition-opacity">
          <button
            type="button"
            onClick={handleCopy}
            className="flex items-center gap-1 text-[11px] text-text-tertiary hover:text-violet-400 transition-colors"
          >
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
            </svg>
            Copy
          </button>
          <span className="text-[11px] text-text-tertiary">
            {formatTime(message.createdAt)}
          </span>
        </div>
      </div>
    </div>
  );
}
