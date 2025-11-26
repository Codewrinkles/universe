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
      profile: (profileId: string) => `${API_BASE_URL}/api/identity/profile/${profileId}`,
      avatar: (profileId: string) => `${API_BASE_URL}/api/identity/profile/${profileId}/avatar`,
    },
  },
  auth: {
    accessTokenKey: "codewrinkles_access_token",
    refreshTokenKey: "codewrinkles_refresh_token",
    userKey: "codewrinkles_user",
  },
} as const;
