import { Skeleton } from "./Skeleton";

/**
 * Skeleton loading state for notification items
 * Mimics the structure of a notification
 */
export function LoadingNotification(): JSX.Element {
  return (
    <div className="px-4 py-3 border-b border-border">
      <div className="flex gap-3">
        {/* Avatar skeleton */}
        <Skeleton variant="circular" className="h-10 w-10 flex-shrink-0" />

        {/* Content skeleton */}
        <div className="flex-1 min-w-0 space-y-2">
          {/* Notification text (name + action) */}
          <Skeleton variant="text" className="h-4 w-3/4" />
          {/* Timestamp */}
          <Skeleton variant="text" className="h-3 w-16" />
        </div>
      </div>
    </div>
  );
}
