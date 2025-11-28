import { useState, useEffect } from "react";
import type { Pulse } from "../../types";
import { pulseApi } from "../../services/pulseApi";
import { PostCard } from "./PostCard";
import { PulseNavigation } from "./PulseNavigation";
import { PulseRightSidebar } from "./PulseRightSidebar";
import { LoadingCard } from "../../components/ui";

export function BookmarksPage(): JSX.Element {
  const [pulses, setPulses] = useState<Pulse[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadBookmarks();
  }, []);

  const loadBookmarks = async (): Promise<void> => {
    try {
      setIsLoading(true);
      setError(null);
      const response = await pulseApi.getBookmarkedPulses({ limit: 50 });
      setPulses(response.pulses);
    } catch (err) {
      console.error("Failed to load bookmarks:", err);
      setError("Failed to load bookmarked pulses");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex justify-center">
      {/* Left Navigation */}
      <aside className="hidden lg:flex w-[320px] flex-shrink-0 justify-end pr-8">
        <div className="w-[240px]">
          <PulseNavigation />
        </div>
      </aside>

      {/* Main Content */}
      <main className="w-full max-w-[600px] border-x border-border lg:w-[600px]">
        {/* Header */}
        <div className="sticky top-0 z-10 border-b border-border bg-surface-page/80 backdrop-blur px-4 py-3">
          <h1 className="text-base font-semibold tracking-tight text-text-primary">
            Bookmarks
          </h1>
        </div>

        {/* Error State */}
        {error && (
          <div className="p-4 border-b border-border bg-red-500/10 text-red-400 text-sm">
            {error}
          </div>
        )}

        {/* Loading State */}
        {isLoading && pulses.length === 0 ? (
          <div>
            {Array.from({ length: 5 }).map((_, i) => (
              <LoadingCard key={i} />
            ))}
          </div>
        ) : pulses.length === 0 ? (
          <div className="p-8 text-center">
            <p className="text-text-secondary text-sm">No bookmarks yet</p>
            <p className="text-text-tertiary text-xs mt-2">
              Save your favorite pulses by clicking the bookmark icon.
            </p>
          </div>
        ) : (
          <div>
            {pulses.map((pulse) => (
              <PostCard key={pulse.id} post={pulse} />
            ))}
          </div>
        )}
      </main>

      {/* Right Sidebar - placeholder for spacing */}
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
