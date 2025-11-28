import { config } from "../config";
import type { GetNotificationsResponse, UnreadCountResponse } from "../types/notification";

/**
 * Helper function to make authenticated API requests
 */
async function apiRequest<T>(url: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem(config.auth.accessTokenKey);

  const response = await fetch(url, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(error || `HTTP ${response.status}: ${response.statusText}`);
  }

  return response.json();
}

export const notificationApi = {
  /**
   * Get paginated notifications for the current user
   */
  getNotifications(params: { offset?: number; limit?: number } = {}): Promise<GetNotificationsResponse> {
    const { offset = 0, limit = 20 } = params;
    const url = new URL(config.api.endpoints.notifications);
    url.searchParams.set("offset", offset.toString());
    url.searchParams.set("limit", limit.toString());

    return apiRequest<GetNotificationsResponse>(url.toString());
  },

  /**
   * Get unread notification count
   */
  getUnreadCount(): Promise<UnreadCountResponse> {
    return apiRequest<UnreadCountResponse>(config.api.endpoints.notificationsUnreadCount);
  },

  /**
   * Mark a single notification as read
   */
  markAsRead(notificationId: string): Promise<{ success: boolean }> {
    return apiRequest<{ success: boolean }>(
      config.api.endpoints.notificationMarkAsRead(notificationId),
      {
        method: "PUT",
      }
    );
  },

  /**
   * Mark all notifications as read
   */
  markAllAsRead(): Promise<{ success: boolean }> {
    return apiRequest<{ success: boolean }>(config.api.endpoints.notificationsMarkAllAsRead, {
      method: "PUT",
    });
  },
};
