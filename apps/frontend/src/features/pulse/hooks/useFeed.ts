/**
 * useFeed hook
 * Fetches and manages paginated pulse feed
 */

import { useState, useEffect, useCallback } from "react";
import type { Pulse } from "../../../types";
import { pulseApi } from "../../../services/pulseApi";
import { useAuth } from "../../../hooks/useAuth";

interface UseFeedResult {
  pulses: Pulse[];
  isLoading: boolean;
  error: string | null;
  hasMore: boolean;
  loadMore: () => void;
  refetch: () => void;
}

export function useFeed(): UseFeedResult {
  const { user } = useAuth();
  const [pulses, setPulses] = useState<Pulse[]>([]);
  const [cursor, setCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchFeed = useCallback(
    async (nextCursor?: string) => {
      setIsLoading(true);
      setError(null);

      try {
        const response = await pulseApi.getFeed({
          cursor: nextCursor,
          limit: 20,
          currentUserId: user?.profileId,
        });

        setPulses((prev) => (nextCursor ? [...prev, ...response.pulses] : response.pulses));
        setCursor(response.nextCursor);
        setHasMore(response.hasMore);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to fetch feed");
      } finally {
        setIsLoading(false);
      }
    },
    [user?.profileId]
  );

  const loadMore = useCallback(() => {
    if (cursor && hasMore && !isLoading) {
      fetchFeed(cursor);
    }
  }, [cursor, hasMore, isLoading, fetchFeed]);

  const refetch = useCallback(() => {
    fetchFeed();
  }, [fetchFeed]);

  // Initial fetch on mount
  useEffect(() => {
    fetchFeed();
  }, [fetchFeed]);

  return { pulses, isLoading, error, hasMore, loadMore, refetch };
}
