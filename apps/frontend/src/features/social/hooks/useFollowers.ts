/**
 * useFollowers hook
 * Fetches and manages paginated followers list
 */

import { useState, useEffect, useCallback } from "react";
import type { FollowerDto } from "../../../types";
import { socialApi } from "../../../services/socialApi";

interface UseFollowersResult {
  followers: FollowerDto[];
  totalCount: number;
  isLoading: boolean;
  error: string | null;
  hasMore: boolean;
  loadMore: () => void;
  refetch: () => void;
}

export function useFollowers(profileId: string): UseFollowersResult {
  const [followers, setFollowers] = useState<FollowerDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [cursor, setCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchFollowers = useCallback(
    async (nextCursor?: string) => {
      setIsLoading(true);
      setError(null);

      try {
        const response = await socialApi.getFollowers(profileId, {
          cursor: nextCursor,
          limit: 20,
        });

        setFollowers((prev) =>
          nextCursor ? [...prev, ...response.followers] : response.followers
        );
        setTotalCount(response.totalCount);
        setCursor(response.nextCursor);
        setHasMore(response.hasMore);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to fetch followers");
      } finally {
        setIsLoading(false);
      }
    },
    [profileId]
  );

  const loadMore = useCallback(() => {
    if (cursor && hasMore && !isLoading) {
      void fetchFollowers(cursor);
    }
  }, [cursor, hasMore, isLoading, fetchFollowers]);

  const refetch = useCallback(() => {
    void fetchFollowers();
  }, [fetchFollowers]);

  // Initial fetch on mount or when profileId changes
  useEffect(() => {
    void fetchFollowers();
  }, [fetchFollowers]);

  return { followers, totalCount, isLoading, error, hasMore, loadMore, refetch };
}
