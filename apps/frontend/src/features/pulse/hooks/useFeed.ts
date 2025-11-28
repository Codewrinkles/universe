/**
 * useFeed hook
 * Fetches and manages paginated pulse feed
 */

import { useState, useEffect, useCallback } from "react";
import type { Pulse } from "../../../types";
import { pulseApi } from "../../../services/pulseApi";

interface UseFeedResult {
  pulses: Pulse[];
  isLoading: boolean;
  error: string | null;
  hasMore: boolean;
  loadMore: () => void;
  refetch: () => void;
}

export function useFeed(): UseFeedResult {
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
        // CurrentUserId is extracted from JWT token on the backend (if authenticated)
        const response = await pulseApi.getFeed({
          cursor: nextCursor,
          limit: 20,
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
    []
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
