/**
 * StreamingIndicator - Shows "Nova is thinking..." with animated dots
 *
 * Displayed while waiting for Nova's response to stream back.
 */
export function StreamingIndicator(): JSX.Element {
  return (
    <div className="flex items-start gap-3 px-4 py-3">
      {/* Nova Avatar */}
      <div className="w-8 h-8 rounded-xl bg-violet-500/20 border border-violet-500/40 flex items-center justify-center flex-shrink-0">
        <span className="text-sm">🤖</span>
      </div>

      {/* Thinking Message */}
      <div className="flex-1">
        <div className="flex items-center gap-2">
          <span className="text-xs font-semibold text-violet-400 uppercase tracking-wide">
            Nova
          </span>
        </div>
        <div className="mt-1 flex items-center gap-1 text-sm text-text-secondary">
          <span>Thinking</span>
          <span className="flex gap-0.5">
            <span className="w-1 h-1 bg-violet-400 rounded-full animate-bounce" style={{ animationDelay: "0ms" }} />
            <span className="w-1 h-1 bg-violet-400 rounded-full animate-bounce" style={{ animationDelay: "150ms" }} />
            <span className="w-1 h-1 bg-violet-400 rounded-full animate-bounce" style={{ animationDelay: "300ms" }} />
          </span>
        </div>
      </div>
    </div>
  );
}
