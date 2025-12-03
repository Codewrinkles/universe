export interface YouTubeButtonProps {
  variant?: "default" | "header" | "landing";
  className?: string;
}

/**
 * YouTube channel link button with brand-consistent styling
 * @param variant - "default" | "header" (compact for header) | "landing" (prominent for landing page)
 */
export function YouTubeButton({ variant = "default", className = "" }: YouTubeButtonProps): JSX.Element {
  const baseStyles = "inline-flex items-center gap-2 transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-brand/50";

  const variantStyles = {
    default: "rounded-full border border-border bg-surface-card1 text-text-secondary px-4 py-2 text-sm hover:border-brand-soft/60 hover:bg-surface-card2 hover:text-brand-soft",
    header: "rounded-full border border-border bg-surface-card1 text-text-secondary p-2 hover:border-brand-soft/60 hover:bg-surface-card2 hover:text-brand-soft",
    landing: "rounded-full border border-border bg-surface-card1 text-text-secondary px-6 py-3 text-base hover:border-brand-soft/60 hover:bg-surface-card2 hover:text-brand-soft hover:scale-105"
  };

  const iconSizes = {
    default: "h-5 w-5",
    header: "h-5 w-5",
    landing: "h-6 w-6"
  };

  return (
    <a
      href="https://www.youtube.com/c/Codewrinkles"
      target="_blank"
      rel="noopener noreferrer"
      className={`${baseStyles} ${variantStyles[variant]} ${className}`}
      aria-label="Visit Codewrinkles on YouTube"
    >
      {/* YouTube Icon SVG */}
      <svg
        className={iconSizes[variant]}
        viewBox="0 0 24 24"
        fill="currentColor"
        aria-hidden="true"
      >
        <path d="M23.498 6.186a3.016 3.016 0 0 0-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 0 0 .502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 0 0 2.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 0 0 2.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"/>
      </svg>
      {variant !== "header" && <span>YouTube</span>}
    </a>
  );
}
