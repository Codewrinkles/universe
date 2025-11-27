import { useState } from "react";
import { Composer } from "./Composer";
import { Feed } from "./Feed";
import { PulseNavigation } from "./PulseNavigation";
import { PulseRightSidebar } from "./PulseRightSidebar";
import { useFeed } from "./hooks/useFeed";
import { useCreatePulse } from "./hooks/useCreatePulse";

export function PulsePage(): JSX.Element {
  const [composerValue, setComposerValue] = useState("");
  const maxChars = 300; // Updated to match backend limit
  const charsLeft = maxChars - composerValue.length;
  const isOverLimit = charsLeft < 0;

  // Fetch feed data
  const { pulses, isLoading, error, hasMore, loadMore, refetch } = useFeed();

  // Create pulse mutation
  const { createPulse, isCreating } = useCreatePulse();

  const handleSubmit = async (): Promise<void> => {
    try {
      await createPulse(composerValue);
      setComposerValue(""); // Clear composer after successful post
      refetch(); // Refresh feed to show new pulse
    } catch (err) {
      // Error is already handled by the hook
      console.error("Failed to create pulse:", err);
    }
  };

  return (
    <div className="flex justify-center">
      {/* Left Navigation - container matches right for symmetry */}
      <aside className="hidden lg:flex w-[320px] flex-shrink-0 justify-end pr-8">
        <div className="w-[240px]">
          <PulseNavigation />
        </div>
      </aside>

      {/* Main Content */}
      <main className="w-full max-w-[600px] border-x border-border lg:w-[600px]">
        {/* Composer */}
        <div className="border-b border-border p-4">
          <Composer
            value={composerValue}
            onChange={setComposerValue}
            maxChars={maxChars}
            isOverLimit={isOverLimit}
            charsLeft={charsLeft}
            onSubmit={handleSubmit}
            isSubmitting={isCreating}
          />
        </div>

        {/* Feed */}
        {error && (
          <div className="p-4 border-b border-border bg-red-500/10 text-red-400 text-sm">
            {error}
          </div>
        )}

        {isLoading && pulses.length === 0 ? (
          <div className="p-8 text-center text-text-secondary">
            Loading feed...
          </div>
        ) : pulses.length === 0 ? (
          <div className="p-8 text-center text-text-secondary">
            No pulses yet. Be the first to post!
          </div>
        ) : (
          <>
            <div className="divide-y divide-border">
              <Feed posts={pulses} />
            </div>

            {/* Load More Button */}
            {hasMore && (
              <div className="p-4 border-t border-border text-center">
                <button
                  type="button"
                  onClick={loadMore}
                  disabled={isLoading}
                  className="px-4 py-2 rounded-full bg-surface-card2 hover:bg-surface-card1 text-brand-soft text-sm font-semibold transition-colors disabled:opacity-50"
                >
                  {isLoading ? "Loading..." : "Load More"}
                </button>
              </div>
            )}
          </>
        )}
      </main>

      {/* Right Sidebar - placeholder for spacing, matches left width */}
      <aside className="hidden lg:block w-[320px] flex-shrink-0 pl-8">
        {/* Empty placeholder - actual content is fixed positioned */}
      </aside>

      {/* Right Sidebar - fixed position */}
      <div className="hidden lg:block fixed top-20 z-10 w-[288px] left-[calc(50%+332px)]">
        <PulseRightSidebar />
      </div>
    </div>
  );
}
