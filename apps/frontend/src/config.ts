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
      // PHASE 1: New RESTful endpoint for getting current user's profile (no ID)
      currentUserProfile: `${API_BASE_URL}/api/identity/profile`,
      // PHASE 2 TODO: Migrate these to RESTful pattern (no profileId), keep for backward compat
      profile: (profileId: string) => `${API_BASE_URL}/api/identity/profile/${profileId}`,
      avatar: (profileId: string) => `${API_BASE_URL}/api/identity/profile/${profileId}/avatar`,
      pulse: `${API_BASE_URL}/api/pulse`,
      pulseById: (id: string) => `${API_BASE_URL}/api/pulse/${id}`,
      pulseDelete: (id: string) => `${API_BASE_URL}/api/pulse/${id}`,
      pulseLike: (id: string) => `${API_BASE_URL}/api/pulse/${id}/like`,
      pulseRepulse: `${API_BASE_URL}/api/pulse/repulse`,
      pulseReply: (parentId: string) => `${API_BASE_URL}/api/pulse/${parentId}/reply`,
      notifications: `${API_BASE_URL}/api/notifications`,
      notificationsUnreadCount: `${API_BASE_URL}/api/notifications/unread-count`,
      notificationMarkAsRead: (id: string) => `${API_BASE_URL}/api/notifications/${id}/read`,
      notificationsMarkAllAsRead: `${API_BASE_URL}/api/notifications/read-all`,
      notificationDelete: (id: string) => `${API_BASE_URL}/api/notifications/${id}`,
      notificationsClearAll: `${API_BASE_URL}/api/notifications/all`,
      pulseThread: (id: string) => `${API_BASE_URL}/api/pulse/${id}/thread`,
      pulsesByAuthor: (profileId: string) => `${API_BASE_URL}/api/pulse/author/${profileId}`,
      socialFollow: (profileId: string) => `${API_BASE_URL}/api/social/${profileId}/follow`,
      socialFollowers: (profileId: string) => `${API_BASE_URL}/api/social/${profileId}/followers`,
      socialFollowing: (profileId: string) => `${API_BASE_URL}/api/social/${profileId}/following`,
      socialIsFollowing: (profileId: string) => `${API_BASE_URL}/api/social/${profileId}/is-following`,
      socialSuggestions: `${API_BASE_URL}/api/social/suggestions`,
      searchHandles: (query: string, limit: number = 10) =>
        `${API_BASE_URL}/api/profile/search?q=${encodeURIComponent(query)}&limit=${limit}`,
      bookmarkPulse: (id: string) => `${API_BASE_URL}/api/pulse/${id}/bookmark`,
      bookmarks: `${API_BASE_URL}/api/bookmarks`,
      // Nova - AI Coach
      novaChat: `${API_BASE_URL}/api/nova/chat`,
      novaChatStream: `${API_BASE_URL}/api/nova/chat/stream`,
      novaSessions: `${API_BASE_URL}/api/nova/sessions`,
      novaSession: (sessionId: string) => `${API_BASE_URL}/api/nova/sessions/${sessionId}`,
      novaProfile: `${API_BASE_URL}/api/nova/profile`,
    },
  },
  auth: {
    accessTokenKey: "codewrinkles_access_token",
    refreshTokenKey: "codewrinkles_refresh_token",
    userKey: "codewrinkles_user",
  },
} as const;
