/**
 * Profile API service
 * Handles profile updates and avatar uploads
 */

import { config } from "../config";
import { apiRequest } from "../utils/api";
import type { UpdateProfileRequest, UpdateProfileResponse, UploadAvatarResponse } from "../types";

export const profileApi = {
  /**
   * Update user profile (name, bio, handle)
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

    const response = await fetch(config.api.endpoints.avatar(profileId), {
      method: "POST",
      body: formData,
      // Don't set Content-Type header - browser will set it with boundary for multipart
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || "Failed to upload avatar");
    }

    return response.json() as Promise<UploadAvatarResponse>;
  },
};
