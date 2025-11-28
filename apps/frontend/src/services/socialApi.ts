/**
 * Social API service
 * Handles follow-related API calls (follow, unfollow, followers, following, suggestions)
 */

import type {
  FollowResult,
  FollowersResponse,
  FollowingResponse,
  SuggestedProfilesResponse,
  IsFollowingResponse,
} from "../types";
import { config } from "../config";
import { apiRequest } from "../utils/api";

export const socialApi = {
  /**
   * Follow a user
   * ProfileId is extracted from JWT token on the backend
   */
  followUser(profileId: string): Promise<FollowResult> {
    return apiRequest<FollowResult>(config.api.endpoints.socialFollow(profileId), {
      method: "POST",
    });
  },

  /**
   * Unfollow a user
   * ProfileId is extracted from JWT token on the backend
   */
  unfollowUser(profileId: string): Promise<FollowResult> {
    return apiRequest<FollowResult>(config.api.endpoints.socialFollow(profileId), {
      method: "DELETE",
    });
  },

  /**
   * Get followers of a profile (paginated)
   */
  getFollowers(
    profileId: string,
    params?: { cursor?: string; limit?: number }
  ): Promise<FollowersResponse> {
    const url = new URL(config.api.endpoints.socialFollowers(profileId));

    if (params?.cursor) {
      url.searchParams.set("cursor", params.cursor);
    }
    if (params?.limit) {
      url.searchParams.set("limit", params.limit.toString());
    }

    return apiRequest<FollowersResponse>(url.toString(), {
      method: "GET",
    });
  },

  /**
   * Get who a profile is following (paginated)
   */
  getFollowing(
    profileId: string,
    params?: { cursor?: string; limit?: number }
  ): Promise<FollowingResponse> {
    const url = new URL(config.api.endpoints.socialFollowing(profileId));

    if (params?.cursor) {
      url.searchParams.set("cursor", params.cursor);
    }
    if (params?.limit) {
      url.searchParams.set("limit", params.limit.toString());
    }

    return apiRequest<FollowingResponse>(url.toString(), {
      method: "GET",
    });
  },

  /**
   * Check if current user is following a profile
   * ProfileId is extracted from JWT token on the backend
   */
  isFollowing(profileId: string): Promise<IsFollowingResponse> {
    return apiRequest<IsFollowingResponse>(
      config.api.endpoints.socialIsFollowing(profileId),
      {
        method: "GET",
      }
    );
  },

  /**
   * Get suggested profiles to follow
   * CurrentUserId is extracted from JWT token on the backend
   */
  getSuggestedProfiles(limit?: number): Promise<SuggestedProfilesResponse> {
    const url = new URL(config.api.endpoints.socialSuggestions);

    if (limit) {
      url.searchParams.set("limit", limit.toString());
    }

    return apiRequest<SuggestedProfilesResponse>(url.toString(), {
      method: "GET",
    });
  },
};
