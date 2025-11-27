/**
 * Pulse API service
 * Handles pulse-related API calls (create, fetch feed, fetch single)
 */

import type { CreatePulseRequest, CreatePulseResponse, FeedResponse, Pulse } from "../types";
import { config } from "../config";
import { apiRequest } from "../utils/api";

export const pulseApi = {
  /**
   * Create a new pulse
   */
  createPulse(data: CreatePulseRequest): Promise<CreatePulseResponse> {
    return apiRequest<CreatePulseResponse>(config.api.endpoints.pulse, {
      method: "POST",
      body: JSON.stringify(data),
    });
  },

  /**
   * Get paginated feed of pulses
   */
  getFeed(params?: { cursor?: string; limit?: number; currentUserId?: string }): Promise<FeedResponse> {
    const url = new URL(config.api.endpoints.pulse);

    if (params?.cursor) {
      url.searchParams.set("cursor", params.cursor);
    }
    if (params?.limit) {
      url.searchParams.set("limit", params.limit.toString());
    }
    if (params?.currentUserId) {
      url.searchParams.set("currentUserId", params.currentUserId);
    }

    return apiRequest<FeedResponse>(url.toString(), {
      method: "GET",
    });
  },

  /**
   * Get single pulse by ID
   */
  getPulse(id: string, currentUserId?: string): Promise<Pulse> {
    const url = new URL(config.api.endpoints.pulseById(id));

    if (currentUserId) {
      url.searchParams.set("currentUserId", currentUserId);
    }

    return apiRequest<Pulse>(url.toString(), {
      method: "GET",
    });
  },
};
