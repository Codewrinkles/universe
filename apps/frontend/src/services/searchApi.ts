/**
 * Search API
 * Handles user search operations
 */

import { apiRequest } from "../utils/api";
import { config } from "../config";

export interface SearchProfileResult {
  profileId: string;
  name: string;
  handle: string | null;
  bio: string | null;
  avatarUrl: string | null;
}

export interface SearchProfilesResponse {
  profiles: SearchProfileResult[];
}

export const searchApi = {
  /**
   * Search for users by name or handle
   */
  async searchProfiles(query: string, limit: number = 20): Promise<SearchProfilesResponse> {
    return apiRequest<SearchProfilesResponse>(
      `${config.api.baseUrl}/api/profile/search?q=${encodeURIComponent(query)}&limit=${limit}`
    );
  },
};
