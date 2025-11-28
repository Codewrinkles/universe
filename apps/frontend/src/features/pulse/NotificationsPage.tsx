import { useNotifications } from "../../hooks/useNotifications";
import { NotificationItem } from "./NotificationItem";
import { PulseNavigation } from "./PulseNavigation";
import { PulseRightSidebar } from "./PulseRightSidebar";
import { LoadingNotification } from "../../components/ui";

export function NotificationsPage(): JSX.Element {
  const { notifications, unreadCount, isLoading, error, markAsRead, markAllAsRead } = useNotifications();

  return (
    <div className="flex justify-center">
      {/* Left Navigation */}
      <aside className="hidden lg:flex w-[320px] flex-shrink-0 justify-end pr-8">
        <div className="w-[240px]">
          <PulseNavigation />
        </div>
      </aside>

      {/* Main Content */}
      <main className="w-full max-w-[600px] border-x border-border lg:w-[600px]">
        {/* Header */}
        <div className="sticky top-0 z-10 border-b border-border bg-surface-page/80 backdrop-blur px-4 py-3">
          <div className="flex items-center justify-between">
            <h1 className="text-base font-semibold tracking-tight text-text-primary">
              Notifications
            </h1>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={() => void markAllAsRead()}
                className="text-xs text-brand-soft hover:text-brand transition-colors font-medium"
              >
                Mark all as read
              </button>
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
              />
            ))}
          </div>
        )}
      </main>

      {/* Right Sidebar - placeholder for spacing */}
      <aside className="hidden lg:block w-[320px] flex-shrink-0 pl-8">
        {/* Empty placeholder - actual content is fixed positioned */}
      </aside>

      {/* Right Sidebar - fixed position */}
      <div className="hidden lg:block fixed top-20 z-10 w-[288px] left-[calc(50%+332px)]">
        <PulseRightSidebar />
      </div>
    </div>
  );
}
