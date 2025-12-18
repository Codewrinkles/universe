/**
 * useInfiniteScroll hook
 * Uses IntersectionObserver to trigger loading more content when a sentinel element becomes visible
 */

import { useEffect, useRef, useCallback } from "react";

interface UseInfiniteScrollOptions {
  /** Whether there are more items to load */
  hasMore: boolean;
  /** Whether a load is currently in progress */
  isLoading: boolean;
  /** Function to call when more items should be loaded */
  onLoadMore: () => void;
  /** Root margin for the intersection observer (default: "200px" - triggers 200px before sentinel is visible) */
  rootMargin?: string;
  /** Threshold for intersection (default: 0.1) */
  threshold?: number;
}

interface UseInfiniteScrollResult {
  /** Ref to attach to the sentinel element at the bottom of your list */
  sentinelRef: React.RefObject<HTMLDivElement>;
}

export function useInfiniteScroll({
  hasMore,
  isLoading,
  onLoadMore,
  rootMargin = "200px",
  threshold = 0.1,
}: UseInfiniteScrollOptions): UseInfiniteScrollResult {
  const sentinelRef = useRef<HTMLDivElement>(null);

  const handleIntersection = useCallback(
    (entries: IntersectionObserverEntry[]) => {
      const entry = entries[0];
      if (entry && entry.isIntersecting && hasMore && !isLoading) {
        onLoadMore();
      }
    },
    [hasMore, isLoading, onLoadMore]
  );

  useEffect(() => {
    const sentinel = sentinelRef.current;
    if (!sentinel) return;

    const observer = new IntersectionObserver(handleIntersection, {
      rootMargin,
      threshold,
    });

    observer.observe(sentinel);

    return () => {
      observer.disconnect();
    };
  }, [handleIntersection, rootMargin, threshold]);

  return { sentinelRef };
}
