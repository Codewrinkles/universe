import { Skeleton } from "./Skeleton";

/**
 * Skeleton loading state for PostCard
 * Mimics the structure of a pulse post
 */
export function LoadingCard(): JSX.Element {
  return (
    <article className="px-4 py-3 border-b border-border">
      <div className="flex gap-3">
        {/* Avatar skeleton */}
        <Skeleton variant="circular" className="h-10 w-10 flex-shrink-0" />

        {/* Content skeleton */}
        <div className="flex-1 min-w-0 space-y-3">
          {/* Header (name, handle, time) */}
          <div className="flex items-center gap-2">
            <Skeleton variant="text" className="h-4 w-24" />
            <Skeleton variant="text" className="h-4 w-20" />
            <Skeleton variant="text" className="h-4 w-12" />
          </div>

          {/* Post content (2-3 lines of text) */}
          <div className="space-y-2">
            <Skeleton variant="text" className="h-4 w-full" />
            <Skeleton variant="text" className="h-4 w-5/6" />
            <Skeleton variant="text" className="h-4 w-3/4" />
          </div>

          {/* Action buttons */}
          <div className="flex items-center gap-4 pt-1">
            <Skeleton variant="text" className="h-8 w-12" />
            <Skeleton variant="text" className="h-8 w-12" />
            <Skeleton variant="text" className="h-8 w-12" />
            <Skeleton variant="text" className="h-8 w-12" />
          </div>
        </div>
      </div>
    </article>
  );
}
