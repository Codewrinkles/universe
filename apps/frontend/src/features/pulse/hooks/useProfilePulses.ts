/**
 * useProfilePulses hook
 * Fetches and manages paginated pulses for a specific author/profile
 */

import { useState, useEffect, useCallback } from "react";
import type { Pulse } from "../../../types";
import { pulseApi } from "../../../services/pulseApi";

interface UseProfilePulsesResult {
  pulses: Pulse[];
  totalCount: number;
  isLoading: boolean;
  error: string | null;
  hasMore: boolean;
  loadMore: () => void;
  refetch: () => void;
  /** Update a pulse in the local state (e.g., after a reply is created) */
  updatePulse: (pulseId: string, updates: Partial<Pulse>) => void;
  /** Remove a pulse from the local state */
  removePulse: (pulseId: string) => void;
}

export function useProfilePulses(profileId: string | null): UseProfilePulsesResult {
  const [pulses, setPulses] = useState<Pulse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [cursor, setCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchPulses = useCallback(
    async (nextCursor?: string) => {
      if (!profileId) return;

      setIsLoading(true);
      setError(null);

      try {
        const response = await pulseApi.getAuthorPulses(profileId, {
          cursor: nextCursor,
          limit: 20,
        });

        setPulses((prev) => (nextCursor ? [...prev, ...response.pulses] : response.pulses));
        setTotalCount(response.totalCount);
        setCursor(response.nextCursor);
        setHasMore(response.hasMore);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to fetch pulses");
      } finally {
        setIsLoading(false);
      }
    },
    [profileId]
  );

  const loadMore = useCallback(() => {
    if (cursor && hasMore && !isLoading) {
      fetchPulses(cursor);
    }
  }, [cursor, hasMore, isLoading, fetchPulses]);

  const refetch = useCallback(() => {
    // Reset state and fetch from the beginning
    setCursor(null);
    setHasMore(true);
    fetchPulses();
  }, [fetchPulses]);

  const updatePulse = useCallback((pulseId: string, updates: Partial<Pulse>) => {
    setPulses((prev) =>
      prev.map((p) => (p.id === pulseId ? { ...p, ...updates } : p))
    );
  }, []);

  const removePulse = useCallback((pulseId: string) => {
    setPulses((prev) => prev.filter((p) => p.id !== pulseId));
    setTotalCount((prev) => Math.max(0, prev - 1));
  }, []);

  // Initial fetch when profileId changes
  useEffect(() => {
    if (profileId) {
      // Reset state for new profile
      setPulses([]);
      setCursor(null);
      setHasMore(true);
      setTotalCount(0);
      fetchPulses();
    }
  }, [profileId, fetchPulses]);

  return {
    pulses,
    totalCount,
    isLoading,
    error,
    hasMore,
    loadMore,
    refetch,
    updatePulse,
    removePulse,
  };
}
