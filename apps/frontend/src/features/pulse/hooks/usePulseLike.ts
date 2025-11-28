/**
 * usePulseLike hook
 * Handles liking/unliking pulses with optimistic updates
 */

import { useState, useCallback } from "react";
import { pulseApi } from "../../../services/pulseApi";
import { useAuth } from "../../../hooks/useAuth";
import { ApiError } from "../../../utils/api";

interface UsePulseLikeResult {
  toggleLike: (pulseId: string, isLiked: boolean) => Promise<void>;
  isLoading: boolean;
  error: string | null;
}

export function usePulseLike(): UsePulseLikeResult {
  const { user } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const toggleLike = useCallback(
    async (pulseId: string, isLiked: boolean): Promise<void> => {
      if (!user?.profileId) {
        throw new Error("User not authenticated");
      }

      setIsLoading(true);
      setError(null);

      try {
        if (isLiked) {
          // Unlike the pulse
          await pulseApi.unlikePulse(pulseId);
        } else {
          // Like the pulse
          await pulseApi.likePulse(pulseId);
        }
      } catch (err) {
        let errorMessage = "Failed to update like status";

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

  return { toggleLike, isLoading, error };
}
