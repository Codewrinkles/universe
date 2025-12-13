import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Feed } from "./Feed";
import { PulseThreeColumnLayout } from "./PulseThreeColumnLayout";
import { LoadingCard, Spinner } from "../../components/ui";
import { config } from "../../config";
import type { Pulse, FeedResponse } from "../../types";

export function HashtagPage(): JSX.Element {
  const { tag } = useParams<{ tag: string }>();
  const [pulses, setPulses] = useState<Pulse[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState<boolean>(false);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [replyingToPulseId, setReplyingToPulseId] = useState<string | null>(null);

  const fetchPulses = async (cursor?: string | null): Promise<void> => {
    if (!tag) return;

    try {
      setIsLoading(true);
      setError(null);

      const token = localStorage.getItem(config.auth.accessTokenKey);

      const url = cursor
        ? `${config.api.baseUrl}/api/pulse/hashtags/${encodeURIComponent(tag)}?cursor=${encodeURIComponent(cursor)}&limit=20`
        : `${config.api.baseUrl}/api/pulse/hashtags/${encodeURIComponent(tag)}?limit=20`;

      const response = await fetch(url, {
        headers: {
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
      });

      if (!response.ok) {
        throw new Error("Failed to fetch pulses");
      }

      const data = await response.json() as FeedResponse;

      if (cursor) {
        // Append to existing pulses
        setPulses((prev) => [...prev, ...data.pulses]);
      } else {
        // Replace pulses (initial load or refetch)
        setPulses(data.pulses);
      }

      setHasMore(data.hasMore);
      setNextCursor(data.nextCursor);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load pulses");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void fetchPulses();
  }, [tag]);

  const handleLoadMore = (): void => {
    if (nextCursor && !isLoading) {
      void fetchPulses(nextCursor);
    }
  };

  const handleReplyClick = (pulseId: string): void => {
    setReplyingToPulseId(pulseId);
  };

  const handleReplyCreated = (): void => {
    setReplyingToPulseId(null);
    void fetchPulses(); // Refresh feed
  };

  const handleDelete = (): void => {
    void fetchPulses(); // Refresh feed
  };

  return (
    <PulseThreeColumnLayout>
      {/* Header */}
      <div className="sticky top-0 z-10 bg-surface-page border-b border-border p-4">
        <h1 className="text-xl font-bold text-text-primary">
          #{tag}
        </h1>
        <p className="text-sm text-text-secondary mt-1">
          Pulses with this hashtag
        </p>
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
        <div className="p-8 text-center text-text-secondary">
          No pulses found with this hashtag yet.
        </div>
      ) : (
        <>
          <div className="divide-y divide-border">
            <Feed
              posts={pulses}
              onFollowChange={() => void fetchPulses()}
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
                onClick={handleLoadMore}
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
    </PulseThreeColumnLayout>
  );
}
