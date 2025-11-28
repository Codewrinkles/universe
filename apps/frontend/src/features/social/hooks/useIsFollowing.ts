/**
 * useIsFollowing hook
 * Checks if current user is following a specific profile
 */

import { useState, useEffect } from "react";
import { socialApi } from "../../../services/socialApi";
import { useAuth } from "../../../hooks/useAuth";

interface UseIsFollowingResult {
  isFollowing: boolean;
  isLoading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useIsFollowing(profileId: string): UseIsFollowingResult {
  const { user } = useAuth();
  const [isFollowing, setIsFollowing] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchFollowStatus = async (): Promise<void> => {
    // Don't fetch if profileId is empty (happens when initialIsFollowing is provided)
    if (!profileId || !user?.profileId) {
      setIsFollowing(false);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const response = await socialApi.isFollowing(profileId);
      setIsFollowing(response.isFollowing);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to check follow status");
      setIsFollowing(false);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void fetchFollowStatus();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [profileId, user?.profileId]);

  return {
    isFollowing,
    isLoading,
    error,
    refetch: fetchFollowStatus,
  };
}
