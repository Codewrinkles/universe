import { useState, useEffect } from "react";
import type { Pulse } from "../../types";
import { pulseApi } from "../../services/pulseApi";
import { PostCard } from "./PostCard";
import { PulseThreeColumnLayout } from "./PulseThreeColumnLayout";
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

  const handleEdit = (): void => {
    // Refetch bookmarks to show updated content
    loadBookmarks();
  };

  return (
    <PulseThreeColumnLayout>
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
            <PostCard key={pulse.id} post={pulse} onEdit={handleEdit} />
          ))}
        </div>
      )}
    </PulseThreeColumnLayout>
  );
}
