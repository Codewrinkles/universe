export interface SkeletonProps {
  className?: string;
  variant?: "text" | "circular" | "rectangular";
}

/**
 * Reusable skeleton loader component for content placeholders
 * Animated shimmer effect matching our dark theme
 */
export function Skeleton({ className = "", variant = "rectangular" }: SkeletonProps): JSX.Element {
  const variantClasses = {
    text: "rounded",
    circular: "rounded-full",
    rectangular: "rounded-xl",
  };

  return (
    <div
      className={`animate-pulse bg-surface-card2 ${variantClasses[variant]} ${className}`}
      role="status"
      aria-label="Loading content"
    >
      <span className="sr-only">Loading...</span>
    </div>
  );
}
