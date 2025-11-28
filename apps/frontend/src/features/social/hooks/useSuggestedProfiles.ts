/**
 * useSuggestedProfiles hook
 * Fetches suggested profiles based on mutual follows (2-hop algorithm)
 */

import { useState, useEffect, useCallback } from "react";
import type { ProfileSuggestion } from "../../../types";
import { socialApi } from "../../../services/socialApi";
import { useAuth } from "../../../hooks/useAuth";

interface UseSuggestedProfilesResult {
  suggestions: ProfileSuggestion[];
  isLoading: boolean;
  error: string | null;
  refetch: () => void;
}

export function useSuggestedProfiles(limit: number = 10): UseSuggestedProfilesResult {
  const { user } = useAuth();
  const [suggestions, setSuggestions] = useState<ProfileSuggestion[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchSuggestions = useCallback(async () => {
    if (!user?.profileId) {
      setSuggestions([]);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const response = await socialApi.getSuggestedProfiles(limit);
      setSuggestions(response.suggestions);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to fetch suggestions");
      setSuggestions([]);
    } finally {
      setIsLoading(false);
    }
  }, [user?.profileId, limit]);

  // Initial fetch on mount or when user changes
  useEffect(() => {
    void fetchSuggestions();
  }, [fetchSuggestions]);

  return { suggestions, isLoading, error, refetch: fetchSuggestions };
}
