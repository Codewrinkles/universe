/**
 * useFollowing hook
 * Fetches and manages paginated following list
 */

import { useState, useEffect, useCallback } from "react";
import type { FollowingDto } from "../../../types";
import { socialApi } from "../../../services/socialApi";

interface UseFollowingResult {
  following: FollowingDto[];
  totalCount: number;
  isLoading: boolean;
  error: string | null;
  hasMore: boolean;
  loadMore: () => void;
  refetch: () => void;
}

export function useFollowing(profileId: string): UseFollowingResult {
  const [following, setFollowing] = useState<FollowingDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [cursor, setCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchFollowing = useCallback(
    async (nextCursor?: string) => {
      setIsLoading(true);
      setError(null);

      try {
        const response = await socialApi.getFollowing(profileId, {
          cursor: nextCursor,
          limit: 20,
        });

        setFollowing((prev) =>
          nextCursor ? [...prev, ...response.following] : response.following
        );
        setTotalCount(response.totalCount);
        setCursor(response.nextCursor);
        setHasMore(response.hasMore);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to fetch following");
      } finally {
        setIsLoading(false);
      }
    },
    [profileId]
  );

  const loadMore = useCallback(() => {
    if (cursor && hasMore && !isLoading) {
      void fetchFollowing(cursor);
    }
  }, [cursor, hasMore, isLoading, fetchFollowing]);

  const refetch = useCallback(() => {
    void fetchFollowing();
  }, [fetchFollowing]);

  // Initial fetch on mount or when profileId changes
  useEffect(() => {
    void fetchFollowing();
  }, [fetchFollowing]);

  return { following, totalCount, isLoading, error, hasMore, loadMore, refetch };
}
