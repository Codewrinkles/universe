export interface SpinnerProps {
  size?: "sm" | "md" | "lg";
  className?: string;
}

/**
 * Reusable spinner component for loading states
 * Sizes: sm (16px), md (24px), lg (32px)
 */
export function Spinner({ size = "md", className = "" }: SpinnerProps): JSX.Element {
  const sizeClasses = {
    sm: "h-4 w-4 border-2",
    md: "h-6 w-6 border-2",
    lg: "h-8 w-8 border-[3px]",
  };

  return (
    <div
      className={`inline-block animate-spin rounded-full border-brand-soft border-t-transparent ${sizeClasses[size]} ${className}`}
      role="status"
      aria-label="Loading"
    >
      <span className="sr-only">Loading...</span>
    </div>
  );
}
