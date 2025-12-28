/**
 * Nova API service
 * Handles Nova-related API calls (summarization for Pulse sharing)
 */

import { config } from "../config";
import { apiRequest } from "../utils/api";

export interface SummarizeForPulseResponse {
  summary: string;
}

export const novaApi = {
  /**
   * Summarize a Nova response for sharing on Pulse
   * Returns a condensed, plain-text summary under 485 characters
   */
  summarizeForPulse(content: string): Promise<SummarizeForPulseResponse> {
    return apiRequest<SummarizeForPulseResponse>(
      config.api.endpoints.novaSummarizeForPulse,
      {
        method: "POST",
        body: JSON.stringify({ content }),
      }
    );
  },
};
