import { useNotifications } from "../../hooks/useNotifications";
import { NotificationItem } from "./NotificationItem";
import { PulseThreeColumnLayout } from "./PulseThreeColumnLayout";
import { LoadingNotification } from "../../components/ui";

export function NotificationsPage(): JSX.Element {
  const { notifications, unreadCount, isLoading, error, markAsRead, markAllAsRead, deleteNotification, clearAll } = useNotifications();

  const handleClearAll = (): void => {
    if (window.confirm("Are you sure you want to delete all notifications? This cannot be undone.")) {
      void clearAll();
    }
  };

  return (
    <PulseThreeColumnLayout>
      {/* Header */}
      <div className="sticky top-0 z-10 border-b border-border bg-surface-page/80 backdrop-blur px-4 py-3">
        <div className="flex items-center justify-between">
          <h1 className="text-base font-semibold tracking-tight text-text-primary">
            Notifications
          </h1>
          {notifications.length > 0 && (
            <div className="flex items-center gap-3">
              {unreadCount > 0 && (
                <button
                  type="button"
                  onClick={() => void markAllAsRead()}
                  className="text-xs text-brand-soft hover:text-brand transition-colors font-medium"
                >
                  Mark all as read
                </button>
              )}
              <button
                type="button"
                onClick={handleClearAll}
                className="text-xs text-text-tertiary hover:text-red-400 transition-colors font-medium"
              >
                Clear all
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Error State */}
      {error && (
        <div className="p-4 border-b border-border bg-red-500/10 text-red-400 text-sm">
          {error}
        </div>
      )}

      {/* Loading State */}
      {isLoading && notifications.length === 0 ? (
        <div>
          {Array.from({ length: 8 }).map((_, i) => (
            <LoadingNotification key={i} />
          ))}
        </div>
      ) : notifications.length === 0 ? (
        <div className="p-8 text-center">
          <p className="text-text-secondary text-sm">No notifications yet</p>
          <p className="text-text-tertiary text-xs mt-2">
            When someone likes, replies, or mentions you, you'll see it here.
          </p>
        </div>
      ) : (
        <div>
          {notifications.map((notification) => (
            <NotificationItem
              key={notification.id}
              notification={notification}
              onMarkAsRead={markAsRead}
              onDelete={deleteNotification}
            />
          ))}
        </div>
      )}
    </PulseThreeColumnLayout>
  );
}
