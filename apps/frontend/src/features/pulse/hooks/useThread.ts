import { useState, useEffect, useCallback } from "react";
import type { Pulse, ThreadResponse } from "../../../types";
import { pulseApi } from "../../../services/pulseApi";

export interface UseThreadResult {
  parentPulse: Pulse | null;
  replies: Pulse[];
  totalReplyCount: number;
  isLoading: boolean;
  error: string | null;
  hasMore: boolean;
  loadMore: () => Promise<void>;
  refetch: () => Promise<void>;
}

export function useThread(pulseId: string): UseThreadResult {
  const [parentPulse, setParentPulse] = useState<Pulse | null>(null);
  const [replies, setReplies] = useState<Pulse[]>([]);
  const [totalReplyCount, setTotalReplyCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);

  const fetchThread = useCallback(
    async (cursor?: string): Promise<void> => {
      setIsLoading(true);
      setError(null);

      try {
        const response: ThreadResponse = await pulseApi.getThread(pulseId, {
          cursor,
          limit: 20,
        });

        if (cursor) {
          // Appending more replies (infinite scroll)
          setReplies((prev) => [...prev, ...response.replies]);
        } else {
          // Initial load or refetch
          setParentPulse(response.parentPulse);
          setReplies(response.replies);
          setTotalReplyCount(response.totalReplyCount);
        }

        setNextCursor(response.nextCursor);
        setHasMore(response.hasMore);
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : "Failed to load thread";
        setError(errorMessage);
      } finally {
        setIsLoading(false);
      }
    },
    [pulseId]
  );

  const loadMore = useCallback(async (): Promise<void> => {
    if (!hasMore || isLoading || !nextCursor) return;
    await fetchThread(nextCursor);
  }, [hasMore, isLoading, nextCursor, fetchThread]);

  const refetch = useCallback(async (): Promise<void> => {
    await fetchThread();
  }, [fetchThread]);

  useEffect(() => {
    void fetchThread();
  }, [fetchThread]);

  return {
    parentPulse,
    replies,
    totalReplyCount,
    isLoading,
    error,
    hasMore,
    loadMore,
    refetch,
  };
}
