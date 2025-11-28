/**
 * Pulse API service
 * Handles pulse-related API calls (create, fetch feed, fetch single)
 */

import type { CreatePulseResponse, FeedResponse, Pulse, ThreadResponse } from "../types";
import { config } from "../config";
import { apiRequest } from "../utils/api";

export const pulseApi = {
  /**
   * Create a new pulse
   */
  createPulse(content: string, image: File | null): Promise<CreatePulseResponse> {
    // Always use FormData (backend expects multipart/form-data)
    const formData = new FormData();
    formData.append("content", content);

    if (image) {
      formData.append("image", image);
    }

    return apiRequest<CreatePulseResponse>(config.api.endpoints.pulse, {
      method: "POST",
      body: formData,
      // Don't set Content-Type header - browser will set it with boundary
      headers: {},
    });
  },

  /**
   * Get paginated feed of pulses
   * CurrentUserId is extracted from JWT token on the backend (if authenticated)
   */
  getFeed(params?: { cursor?: string; limit?: number }): Promise<FeedResponse> {
    const url = new URL(config.api.endpoints.pulse);

    if (params?.cursor) {
      url.searchParams.set("cursor", params.cursor);
    }
    if (params?.limit) {
      url.searchParams.set("limit", params.limit.toString());
    }

    return apiRequest<FeedResponse>(url.toString(), {
      method: "GET",
    });
  },

  /**
   * Get single pulse by ID
   * CurrentUserId is extracted from JWT token on the backend (if authenticated)
   */
  getPulse(id: string): Promise<Pulse> {
    const url = new URL(config.api.endpoints.pulseById(id));

    return apiRequest<Pulse>(url.toString(), {
      method: "GET",
    });
  },

  /**
   * Like a pulse
   * ProfileId is extracted from JWT token on the backend
   */
  likePulse(id: string): Promise<{ success: boolean }> {
    return apiRequest<{ success: boolean }>(config.api.endpoints.pulseLike(id), {
      method: "POST",
    });
  },

  /**
   * Unlike a pulse
   * ProfileId is extracted from JWT token on the backend
   */
  unlikePulse(id: string): Promise<{ success: boolean }> {
    return apiRequest<{ success: boolean }>(config.api.endpoints.pulseLike(id), {
      method: "DELETE",
    });
  },

  /**
   * Create a reply to a pulse
   * ProfileId is extracted from JWT token on the backend
   */
  createReply(parentPulseId: string, content: string, image: File | null): Promise<CreatePulseResponse> {
    // Use FormData for reply (same as createPulse)
    if (image) {
      const formData = new FormData();
      formData.append("content", content);
      formData.append("image", image);

      return apiRequest<CreatePulseResponse>(config.api.endpoints.pulseReply(parentPulseId), {
        method: "POST",
        body: formData,
        headers: {},
      });
    }

    // No image - use FormData with just content
    const formData = new FormData();
    formData.append("content", content);

    return apiRequest<CreatePulseResponse>(config.api.endpoints.pulseReply(parentPulseId), {
      method: "POST",
      body: formData,
      headers: {},
    });
  },

  /**
   * Get thread (parent pulse + replies with pagination)
   * CurrentUserId is extracted from JWT token on the backend (if authenticated)
   */
  getThread(pulseId: string, params?: { cursor?: string; limit?: number }): Promise<ThreadResponse> {
    const url = new URL(config.api.endpoints.pulseThread(pulseId));

    if (params?.cursor) {
      url.searchParams.set("cursor", params.cursor);
    }
    if (params?.limit) {
      url.searchParams.set("limit", params.limit.toString());
    }

    return apiRequest<ThreadResponse>(url.toString(), {
      method: "GET",
    });
  },
};
