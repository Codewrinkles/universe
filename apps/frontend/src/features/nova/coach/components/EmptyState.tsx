/**
 * EmptyState - Cody introduction shown when starting a new conversation
 *
 * This is the welcome screen users see when they land on /nova/c/new.
 * It introduces Cody (the AI coach) and sets expectations.
 */
export function EmptyState(): JSX.Element {
  return (
    <div className="flex flex-col items-center justify-center text-center px-4 py-8">
      {/* Cody Avatar */}
      <div className="w-16 h-16 rounded-2xl bg-violet-500/20 border border-violet-500/40 flex items-center justify-center mb-4">
        <span className="text-3xl">🤖</span>
      </div>

      {/* Welcome Message */}
      <h1 className="text-xl font-semibold text-text-primary mb-2">
        Hey! I'm Cody, your learning coach.
      </h1>
      <p className="text-sm text-text-secondary max-w-md">
        Ask me anything about software development, architecture, or technical concepts.
        I'm here to help you learn and grow.
      </p>
    </div>
  );
}
