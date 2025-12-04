/**
 * Hook to fetch and manage current user's profile data
 * Used by Settings page to load fresh profile data from API
 *
 * PHASE 1: New hook using RESTful GET /api/identity/profile endpoint
 */

import { useState, useEffect } from "react";
import { profileApi } from "../services/profileApi";
import type { GetCurrentUserProfileResponse } from "../types";

interface UseProfileResult {
  profile: GetCurrentUserProfileResponse | null;
  isLoading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

/**
 * Fetches the current authenticated user's profile from the API
 * Automatically fetches on mount
 *
 * @returns Profile data, loading state, error state, and refetch function
 */
export function useProfile(): UseProfileResult {
  const [profile, setProfile] = useState<GetCurrentUserProfileResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchProfile = async (): Promise<void> => {
    setIsLoading(true);
    setError(null);

    try {
      const data = await profileApi.getCurrentUserProfile();
      setProfile(data);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "Failed to load profile";
      setError(errorMessage);
      console.error("Error fetching profile:", err);
    } finally {
      setIsLoading(false);
    }
  };

  // Fetch profile on mount
  useEffect(() => {
    void fetchProfile();
  }, []);

  return {
    profile,
    isLoading,
    error,
    refetch: fetchProfile,
  };
}
