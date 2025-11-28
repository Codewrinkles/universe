import { Skeleton } from "./Skeleton";

/**
 * Skeleton loading state for profile header
 * Mimics the structure of the profile page header
 */
export function LoadingProfile(): JSX.Element {
  return (
    <div className="border-b border-border">
      {/* Cover image skeleton */}
      <Skeleton variant="rectangular" className="h-48 w-full rounded-none" />

      {/* Profile info */}
      <div className="px-4 pb-4">
        {/* Avatar overlapping cover */}
        <div className="-mt-16 mb-4">
          <Skeleton variant="circular" className="h-32 w-32 border-4 border-surface-page" />
        </div>

        {/* Name and handle */}
        <div className="space-y-2 mb-3">
          <Skeleton variant="text" className="h-6 w-48" />
          <Skeleton variant="text" className="h-4 w-32" />
        </div>

        {/* Bio */}
        <div className="space-y-2 mb-3">
          <Skeleton variant="text" className="h-4 w-full" />
          <Skeleton variant="text" className="h-4 w-3/4" />
        </div>

        {/* Stats (following, followers) */}
        <div className="flex items-center gap-4">
          <Skeleton variant="text" className="h-4 w-24" />
          <Skeleton variant="text" className="h-4 w-24" />
        </div>
      </div>
    </div>
  );
}
