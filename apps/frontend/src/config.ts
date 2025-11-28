/**
 * Application configuration
 * API endpoints and environment-specific settings
 */

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "https://localhost:7280";

export const config = {
  api: {
    baseUrl: API_BASE_URL,
    endpoints: {
      register: `${API_BASE_URL}/api/identity/register`,
      login: `${API_BASE_URL}/api/identity/login`,
      changePassword: `${API_BASE_URL}/api/identity/change-password`,
      profile: (profileId: string) => `${API_BASE_URL}/api/identity/profile/${profileId}`,
      avatar: (profileId: string) => `${API_BASE_URL}/api/identity/profile/${profileId}/avatar`,
      pulse: `${API_BASE_URL}/api/pulse`,
      pulseById: (id: string) => `${API_BASE_URL}/api/pulse/${id}`,
      pulseLike: (id: string) => `${API_BASE_URL}/api/pulse/${id}/like`,
    },
  },
  auth: {
    accessTokenKey: "codewrinkles_access_token",
    refreshTokenKey: "codewrinkles_refresh_token",
    userKey: "codewrinkles_user",
  },
} as const;
