import { useState, useEffect, useCallback } from "react";
import { config } from "../../../config";
import type { LearnerProfile, LearningStyle, PreferredPace } from "../types";

interface UseLearnerProfileResult {
  profile: LearnerProfile | null;
  isLoading: boolean;
  error: string | null;
  updateProfile: (data: UpdateLearnerProfileData) => Promise<void>;
  isUpdating: boolean;
  refetch: () => Promise<void>;
}

export interface UpdateLearnerProfileData {
  currentRole?: string | null;
  experienceYears?: number | null;
  primaryTechStack?: string | null;
  currentProject?: string | null;
  learningGoals?: string | null;
  learningStyle?: LearningStyle | null;
  preferredPace?: PreferredPace | null;
}

export function useLearnerProfile(): UseLearnerProfileResult {
  const [profile, setProfile] = useState<LearnerProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchProfile = useCallback(async () => {
    const token = localStorage.getItem(config.auth.accessTokenKey);
    if (!token) {
      setIsLoading(false);
      return;
    }

    try {
      setError(null);
      const response = await fetch(config.api.endpoints.novaProfile, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error("Failed to fetch learner profile");
      }

      const data = await response.json();
      setProfile(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "An error occurred");
    } finally {
      setIsLoading(false);
    }
  }, []);

  const updateProfile = useCallback(async (data: UpdateLearnerProfileData) => {
    const token = localStorage.getItem(config.auth.accessTokenKey);
    if (!token) {
      throw new Error("Not authenticated");
    }

    setIsUpdating(true);
    setError(null);

    try {
      const response = await fetch(config.api.endpoints.novaProfile, {
        method: "PUT",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(data),
      });

      if (!response.ok) {
        throw new Error("Failed to update learner profile");
      }

      const updatedProfile = await response.json();
      setProfile(updatedProfile);
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : "An error occurred";
      setError(errorMessage);
      throw err;
    } finally {
      setIsUpdating(false);
    }
  }, []);

  useEffect(() => {
    void fetchProfile();
  }, [fetchProfile]);

  return {
    profile,
    isLoading,
    error,
    updateProfile,
    isUpdating,
    refetch: fetchProfile,
  };
}
