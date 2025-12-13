/**
 * useEditPulse hook
 * Handles editing pulse content
 */

import { useState, useCallback } from "react";
import { pulseApi } from "../../../services/pulseApi";
import { useAuth } from "../../../hooks/useAuth";

interface EditPulseResult {
  success: boolean;
  content: string;
  updatedAt: string;
}

interface UseEditPulseResult {
  editPulse: (pulseId: string, content: string) => Promise<EditPulseResult>;
  isEditing: boolean;
  error: string | null;
}

export function useEditPulse(): UseEditPulseResult {
  const { user } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const editPulse = useCallback(
    async (pulseId: string, content: string): Promise<EditPulseResult> => {
      if (!user?.profileId) {
        throw new Error("User not authenticated");
      }

      setIsEditing(true);
      setError(null);

      try {
        const response = await pulseApi.editPulse(pulseId, content);
        return response;
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to edit pulse";
        setError(errorMessage);
        throw new Error(errorMessage);
      } finally {
        setIsEditing(false);
      }
    },
    [user?.profileId]
  );

  return { editPulse, isEditing, error };
}
