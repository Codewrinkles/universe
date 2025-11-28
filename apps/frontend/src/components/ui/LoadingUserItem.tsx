import { Skeleton } from "./Skeleton";

/**
 * Skeleton loading state for user list items
 * Used in followers/following lists
 */
export function LoadingUserItem(): JSX.Element {
  return (
    <div className="border-b border-border p-4">
      <div className="flex items-start gap-3">
        {/* Avatar skeleton */}
        <Skeleton variant="circular" className="h-12 w-12 flex-shrink-0" />

        {/* User info skeleton */}
        <div className="flex-1 min-w-0 space-y-2">
          {/* Name */}
          <Skeleton variant="text" className="h-4 w-32" />
          {/* Handle */}
          <Skeleton variant="text" className="h-3 w-24" />
          {/* Bio */}
          <Skeleton variant="text" className="h-3 w-full" />
          <Skeleton variant="text" className="h-3 w-3/4" />
        </div>
      </div>
    </div>
  );
}
