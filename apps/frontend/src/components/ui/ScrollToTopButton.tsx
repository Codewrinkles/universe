/**
 * ScrollToTopButton component
 * A floating action button that appears when user scrolls down and scrolls to top on click
 */

import { useScrollPosition } from "../../hooks/useScrollPosition";

interface ScrollToTopButtonProps {
  /** Scroll threshold in pixels before button appears (default: 400) */
  threshold?: number;
}

export function ScrollToTopButton({ threshold = 400 }: ScrollToTopButtonProps): JSX.Element {
  const { isScrolled, scrollToTop } = useScrollPosition({ threshold });

  return (
    <button
      type="button"
      onClick={scrollToTop}
      aria-label="Scroll to top"
      className={`
        fixed bottom-20 right-4 lg:bottom-6 lg:right-6 z-40
        flex items-center justify-center w-11 h-11 lg:w-12 lg:h-12
        rounded-full bg-surface-card2 border border-border
        text-text-secondary shadow-lg
        hover:bg-surface-card1 hover:text-brand-soft
        hover:border-brand-soft/50 hover:shadow-xl
        focus:outline-none focus:ring-2 focus:ring-brand
        transition-all duration-200
        ${isScrolled ? "opacity-100 visible translate-y-0" : "opacity-0 invisible translate-y-4"}
      `}
    >
      <span className="text-xl">↑</span>
    </button>
  );
}
