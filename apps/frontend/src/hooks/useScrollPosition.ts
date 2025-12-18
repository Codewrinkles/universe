/**
 * useScrollPosition hook
 * Tracks scroll position and provides utilities for scroll-based UI interactions
 */

import { useState, useEffect, useCallback } from "react";

interface UseScrollPositionOptions {
  /** Threshold in pixels before scroll state changes (default: 400) */
  threshold?: number;
}

interface UseScrollPositionResult {
  /** Current scroll position from top in pixels */
  scrollY: number;
  /** Whether the user has scrolled past the threshold */
  isScrolled: boolean;
  /** Scroll to top with smooth animation */
  scrollToTop: () => void;
}

export function useScrollPosition({
  threshold = 400,
}: UseScrollPositionOptions = {}): UseScrollPositionResult {
  const [scrollY, setScrollY] = useState(0);
  const [isScrolled, setIsScrolled] = useState(false);

  useEffect(() => {
    const handleScroll = (): void => {
      const currentScrollY = window.scrollY;
      setScrollY(currentScrollY);
      setIsScrolled(currentScrollY > threshold);
    };

    // Check initial scroll position
    handleScroll();

    window.addEventListener("scroll", handleScroll, { passive: true });

    return () => {
      window.removeEventListener("scroll", handleScroll);
    };
  }, [threshold]);

  const scrollToTop = useCallback(() => {
    window.scrollTo({
      top: 0,
      behavior: "smooth",
    });
  }, []);

  return { scrollY, isScrolled, scrollToTop };
}
