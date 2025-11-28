/**
 * useFollow hook
 * Handles following/unfollowing users
 */

import { useState, useCallback } from "react";
import { socialApi } from "../../../services/socialApi";
import { useAuth } from "../../../hooks/useAuth";
import { ApiError } from "../../../utils/api";

interface UseFollowResult {
  follow: (profileId: string) => Promise<void>;
  unfollow: (profileId: string) => Promise<void>;
  isLoading: boolean;
  error: string | null;
}

export function useFollow(): UseFollowResult {
  const { user } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const follow = useCallback(
    async (profileId: string): Promise<void> => {
      if (!user?.profileId) {
        throw new Error("User not authenticated");
      }

      setIsLoading(true);
      setError(null);

      try {
        await socialApi.followUser(profileId);
      } catch (err) {
        let errorMessage = "Failed to follow user";

        if (err instanceof ApiError) {
          errorMessage = err.message;
        } else if (err instanceof Error) {
          errorMessage = err.message;
        }

        setError(errorMessage);
        throw new Error(errorMessage);
      } finally {
        setIsLoading(false);
      }
    },
    [user?.profileId]
  );

  const unfollow = useCallback(
    async (profileId: string): Promise<void> => {
      if (!user?.profileId) {
        throw new Error("User not authenticated");
      }

      setIsLoading(true);
      setError(null);

      try {
        await socialApi.unfollowUser(profileId);
      } catch (err) {
        let errorMessage = "Failed to unfollow user";

        if (err instanceof ApiError) {
          errorMessage = err.message;
        } else if (err instanceof Error) {
          errorMessage = err.message;
        }

        setError(errorMessage);
        throw new Error(errorMessage);
      } finally {
        setIsLoading(false);
      }
    },
    [user?.profileId]
  );

  return { follow, unfollow, isLoading, error };
}
