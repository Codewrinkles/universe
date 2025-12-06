import { useState, useEffect, useCallback } from "react";
import type { Notification } from "../types/notification";
import { notificationApi } from "../services/notificationApi";

interface UseNotificationsResult {
  notifications: Notification[];
  unreadCount: number;
  isLoading: boolean;
  error: string | null;
  refetch: () => void;
  markAsRead: (notificationId: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  deleteNotification: (notificationId: string) => Promise<void>;
  clearAll: () => Promise<void>;
}

export function useNotifications(): UseNotificationsResult {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchNotifications = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await notificationApi.getNotifications({ offset: 0, limit: 50 });
      setNotifications(response.notifications);
      setUnreadCount(response.unreadCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to fetch notifications");
    } finally {
      setIsLoading(false);
    }
  }, []);

  const markAsRead = useCallback(async (notificationId: string) => {
    try {
      await notificationApi.markAsRead(notificationId);

      // Optimistic update
      setNotifications((prev) =>
        prev.map((n) => (n.id === notificationId ? { ...n, isRead: true } : n))
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch (err) {
      console.error("Failed to mark notification as read:", err);
    }
  }, []);

  const markAllAsRead = useCallback(async () => {
    try {
      await notificationApi.markAllAsRead();

      // Optimistic update
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      setUnreadCount(0);
    } catch (err) {
      console.error("Failed to mark all notifications as read:", err);
    }
  }, []);

  const deleteNotification = useCallback(async (notificationId: string) => {
    // Save current state for rollback
    const previousNotifications = notifications;
    const previousUnreadCount = unreadCount;

    // Find the notification to check if it was unread
    const notification = notifications.find((n) => n.id === notificationId);
    const wasUnread = notification ? !notification.isRead : false;

    try {
      // Optimistic update - remove notification
      setNotifications((prev) => prev.filter((n) => n.id !== notificationId));
      if (wasUnread) {
        setUnreadCount((prev) => Math.max(0, prev - 1));
      }

      // Call API
      await notificationApi.deleteNotification(notificationId);
    } catch (err) {
      // Rollback on error
      setNotifications(previousNotifications);
      setUnreadCount(previousUnreadCount);
      setError("Failed to delete notification");
      console.error("Failed to delete notification:", err);
    }
  }, [notifications, unreadCount]);

  const clearAll = useCallback(async () => {
    // Save current state for rollback
    const previousNotifications = notifications;
    const previousUnreadCount = unreadCount;

    try {
      // Optimistic update - clear all
      setNotifications([]);
      setUnreadCount(0);

      // Call API
      await notificationApi.clearAll();
    } catch (err) {
      // Rollback on error
      setNotifications(previousNotifications);
      setUnreadCount(previousUnreadCount);
      setError("Failed to clear all notifications");
      console.error("Failed to clear all notifications:", err);
    }
  }, [notifications, unreadCount]);

  // Initial fetch
  useEffect(() => {
    fetchNotifications();
  }, [fetchNotifications]);

  return {
    notifications,
    unreadCount,
    isLoading,
    error,
    refetch: fetchNotifications,
    markAsRead,
    markAllAsRead,
    deleteNotification,
    clearAll,
  };
}

/**
 * Hook for polling unread count (used in header/navigation)
 */
export function useUnreadNotificationCount(): number {
  const [unreadCount, setUnreadCount] = useState(0);

  useEffect(() => {
    const fetchCount = async (): Promise<void> => {
      try {
        const response = await notificationApi.getUnreadCount();
        setUnreadCount(response.unreadCount);
      } catch (err) {
        console.error("Failed to fetch unread count:", err);
      }
    };

    // Fetch immediately
    fetchCount();

    // Poll every 30 seconds
    const interval = setInterval(fetchCount, 30000);

    return () => clearInterval(interval);
  }, []);

  return unreadCount;
}
