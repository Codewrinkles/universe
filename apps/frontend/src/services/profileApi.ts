/**
 * Profile API service
 * Handles profile updates and avatar uploads
 */

import { config } from "../config";
import { apiRequest } from "../utils/api";
import type { UpdateProfileRequest, UpdateProfileResponse, UploadAvatarResponse, GetCurrentUserProfileResponse } from "../types";

export const profileApi = {
  /**
   * PHASE 1: Get current authenticated user's profile
   * No profileId parameter needed - extracted from JWT token on backend
   * Used by Settings page to load current profile data for editing
   */
  getCurrentUserProfile: async (): Promise<GetCurrentUserProfileResponse> => {
    return apiRequest<GetCurrentUserProfileResponse>(config.api.endpoints.currentUserProfile, {
      method: "GET",
    });
  },

  /**
   * Update user profile (name, bio, handle)
   * PHASE 2 TODO: Migrate to /api/identity/profile (no profileId)
   */
  updateProfile: async (
    profileId: string,
    data: UpdateProfileRequest
  ): Promise<UpdateProfileResponse> => {
    return apiRequest<UpdateProfileResponse>(config.api.endpoints.profile(profileId), {
      method: "PUT",
      body: JSON.stringify(data),
    });
  },

  /**
   * Upload avatar image
   * The backend will resize to 500x500
   */
  uploadAvatar: async (profileId: string, file: File): Promise<UploadAvatarResponse> => {
    const formData = new FormData();
    formData.append("file", file);

    // Use apiRequest to automatically include Authorization header
    return apiRequest<UploadAvatarResponse>(config.api.endpoints.avatar(profileId), {
      method: "POST",
      body: formData,
      // Don't set Content-Type header - browser will set it with boundary for multipart
    });
  },
};
