export function TypingIndicator(): JSX.Element {
  return (
    <div className="mt-1 flex justify-start">
      <div className="inline-flex items-center gap-1 rounded-full border border-border bg-surface-card2 px-2 py-1 text-[10px] text-text-tertiary">
        <span>Twin is thinking</span>
        <span className="flex gap-[3px]">
          <span className="typing-dot h-1 w-1 rounded-full bg-text-tertiary"></span>
          <span className="typing-dot h-1 w-1 rounded-full bg-text-tertiary"></span>
          <span className="typing-dot h-1 w-1 rounded-full bg-text-tertiary"></span>
        </span>
      </div>
    </div>
  );
}
