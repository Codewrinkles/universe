import { useState } from "react";
import { UnifiedComposer } from "./UnifiedComposer";
import { Feed } from "./Feed";
import { PulseNavigation } from "./PulseNavigation";
import { PulseRightSidebar } from "./PulseRightSidebar";
import { useFeed } from "./hooks/useFeed";
import { useAuth } from "../../hooks/useAuth";
import { LoadingCard, Spinner } from "../../components/ui";

export function PulsePage(): JSX.Element {
  const { user } = useAuth();
  const [replyingToPulseId, setReplyingToPulseId] = useState<string | null>(null);

  // Fetch feed data
  const { pulses, isLoading, error, hasMore, loadMore, refetch } = useFeed();

  const handleReplyClick = (pulseId: string): void => {
    setReplyingToPulseId(pulseId);
  };

  const handleReplyCreated = (): void => {
    setReplyingToPulseId(null);
    refetch(); // Refresh feed to show updated reply counts
  };

  const handleDelete = (): void => {
    // Refetch feed to remove deleted pulse
    refetch();
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
        {/* Composer - only show if authenticated */}
        {user && (
          <div className="border-b border-border p-4">
            <UnifiedComposer
              mode="post"
              onSuccess={refetch}
              placeholder="What's happening?"
              rows={3}
              focusedRows={8}
            />
          </div>
        )}

        {/* Feed */}
        {error && (
          <div className="p-4 border-b border-border bg-red-500/10 text-red-400 text-sm">
            {error}
          </div>
        )}

        {isLoading && pulses.length === 0 ? (
          <div>
            {Array.from({ length: 5 }).map((_, i) => (
              <LoadingCard key={i} />
            ))}
          </div>
        ) : pulses.length === 0 ? (
          <div className="p-8 text-center text-text-secondary">
            No pulses yet. Be the first to post!
          </div>
        ) : (
          <>
            <div className="divide-y divide-border">
              <Feed
                posts={pulses}
                onFollowChange={refetch}
                onReplyClick={handleReplyClick}
                replyingToPulseId={replyingToPulseId}
                onReplyCreated={handleReplyCreated}
                onDelete={handleDelete}
              />
            </div>

            {/* Load More Button */}
            {hasMore && (
              <div className="p-4 border-t border-border text-center">
                <button
                  type="button"
                  onClick={loadMore}
                  disabled={isLoading}
                  className="px-4 py-2 rounded-full bg-surface-card2 hover:bg-surface-card1 text-brand-soft text-sm font-semibold transition-colors disabled:opacity-50 flex items-center justify-center gap-2 mx-auto"
                >
                  {isLoading && <Spinner size="sm" />}
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
