import { useEffect, useRef, useState } from "react";

interface UseStickyScrollOptions {
  /** Offset from the top of the viewport when sticking at top (e.g., header height) */
  topOffset?: number;
  /** Offset from the bottom of the viewport when sticking at bottom */
  bottomOffset?: number;
}

/**
 * Twitter-style sticky scroll behavior for sidebars taller than viewport.
 *
 * - Scrolling DOWN: sidebar scrolls with page until its bottom is visible, then sticks
 * - Scrolling UP: sidebar scrolls with page until its top reaches topOffset, then sticks
 * - Short sidebars: always sticky at topOffset
 */
export function useStickyScroll<T extends HTMLElement>(
  options: UseStickyScrollOptions = {}
): {
  ref: React.RefObject<T>;
  style: React.CSSProperties;
} {
  const { topOffset = 80, bottomOffset = 20 } = options;

  const ref = useRef<T>(null);
  const [stickyTop, setStickyTop] = useState(topOffset);

  // Track scroll state in refs to avoid re-renders on every scroll
  const lastScrollY = useRef(0);
  const currentTop = useRef(topOffset);

  useEffect(() => {
    const element = ref.current;
    if (!element) return;

    const updateStickyTop = (): void => {
      const scrollY = window.scrollY;
      const viewportHeight = window.innerHeight;
      const elementHeight = element.offsetHeight;
      const availableHeight = viewportHeight - topOffset - bottomOffset;

      // If sidebar fits in viewport, always stick at top
      if (elementHeight <= availableHeight) {
        if (currentTop.current !== topOffset) {
          currentTop.current = topOffset;
          setStickyTop(topOffset);
        }
        lastScrollY.current = scrollY;
        return;
      }

      // Calculate bounds for the sticky top value
      const minTop = viewportHeight - elementHeight - bottomOffset; // Bottom anchored
      const maxTop = topOffset; // Top anchored

      // Calculate scroll delta
      const scrollDelta = scrollY - lastScrollY.current;

      // Adjust current top based on scroll direction
      // Scrolling down (positive delta) = decrease top (sidebar moves up relative to viewport)
      // Scrolling up (negative delta) = increase top (sidebar moves down relative to viewport)
      let newTop = currentTop.current - scrollDelta;

      // Clamp to bounds
      newTop = Math.max(minTop, Math.min(maxTop, newTop));

      // Only update state if changed significantly (avoid floating point noise)
      if (Math.abs(newTop - currentTop.current) > 0.5) {
        currentTop.current = newTop;
        setStickyTop(newTop);
      }

      lastScrollY.current = scrollY;
    };

    // Initial calculation
    lastScrollY.current = window.scrollY;
    updateStickyTop();

    // Throttled scroll handler using requestAnimationFrame
    let ticking = false;
    const handleScroll = (): void => {
      if (!ticking) {
        window.requestAnimationFrame(() => {
          updateStickyTop();
          ticking = false;
        });
        ticking = true;
      }
    };

    window.addEventListener("scroll", handleScroll, { passive: true });
    window.addEventListener("resize", updateStickyTop);

    return () => {
      window.removeEventListener("scroll", handleScroll);
      window.removeEventListener("resize", updateStickyTop);
    };
  }, [topOffset, bottomOffset]);

  const style: React.CSSProperties = {
    position: "sticky",
    top: stickyTop,
    height: "fit-content",
    alignSelf: "flex-start",
  };

  return { ref, style };
}
