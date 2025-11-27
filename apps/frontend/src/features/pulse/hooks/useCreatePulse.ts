/**
 * useCreatePulse hook
 * Handles creating new pulses
 */

import { useState, useCallback } from "react";
import type { CreatePulseResponse } from "../../../types";
import { pulseApi } from "../../../services/pulseApi";
import { useAuth } from "../../../hooks/useAuth";

interface UseCreatePulseResult {
  createPulse: (content: string) => Promise<CreatePulseResponse>;
  isCreating: boolean;
  error: string | null;
}

export function useCreatePulse(): UseCreatePulseResult {
  const { user } = useAuth();
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createPulse = useCallback(
    async (content: string): Promise<CreatePulseResponse> => {
      if (!user?.profileId) {
        throw new Error("User not authenticated");
      }

      setIsCreating(true);
      setError(null);

      try {
        const response = await pulseApi.createPulse({
          authorId: user.profileId,
          content,
        });

        return response;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to create pulse";
        setError(errorMessage);
        throw new Error(errorMessage);
      } finally {
        setIsCreating(false);
      }
    },
    [user?.profileId]
  );

  return { createPulse, isCreating, error };
}
