/**
 * useCreateRepulse hook
 * Handles creating repulses (quote/repost with commentary)
 */

import { useState, useCallback } from "react";
import type { CreatePulseResponse } from "../../../types";
import { pulseApi } from "../../../services/pulseApi";
import { useAuth } from "../../../hooks/useAuth";

interface UseCreateRepulseResult {
  createRepulse: (repulsedPulseId: string, content: string, image?: File | null) => Promise<CreatePulseResponse>;
  isCreating: boolean;
  error: string | null;
}

export function useCreateRepulse(): UseCreateRepulseResult {
  const { user } = useAuth();
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createRepulse = useCallback(
    async (repulsedPulseId: string, content: string, image?: File | null): Promise<CreatePulseResponse> => {
      if (!user?.profileId) {
        throw new Error("User not authenticated");
      }

      setIsCreating(true);
      setError(null);

      try {
        // AuthorId is extracted from JWT token on the backend
        const response = await pulseApi.createRepulse(repulsedPulseId, content, image ?? null);

        return response;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to create repulse";
        setError(errorMessage);
        throw new Error(errorMessage);
      } finally {
        setIsCreating(false);
      }
    },
    [user?.profileId]
  );

  return { createRepulse, isCreating, error };
}
