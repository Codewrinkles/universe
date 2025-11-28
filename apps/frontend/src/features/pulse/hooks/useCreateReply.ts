import { useState } from "react";
import { pulseApi } from "../../../services/pulseApi";

export interface UseCreateReplyResult {
  createReply: (
    parentPulseId: string,
    content: string,
    image: File | null
  ) => Promise<void>;
  isCreating: boolean;
  error: string | null;
}

export function useCreateReply(): UseCreateReplyResult {
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createReply = async (
    parentPulseId: string,
    content: string,
    image: File | null
  ): Promise<void> => {
    setIsCreating(true);
    setError(null);

    try {
      await pulseApi.createReply(parentPulseId, content, image);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "Failed to create reply";
      setError(errorMessage);
      throw err;
    } finally {
      setIsCreating(false);
    }
  };

  return {
    createReply,
    isCreating,
    error,
  };
}
