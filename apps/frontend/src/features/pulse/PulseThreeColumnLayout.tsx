import type { ReactNode } from "react";
import { useAuth } from "../../hooks/useAuth";
import { useStickyScroll } from "../../hooks/useStickyScroll";
import { PulseNavigation } from "./PulseNavigation";
import { PulseRightSidebar } from "./PulseRightSidebar";

interface PulseThreeColumnLayoutProps {
  children: ReactNode;
  /** Hide sidebars even for authenticated users (e.g., for full-width pages) */
  hideSidebars?: boolean;
}

/**
 * Shared 3-column layout for all Pulse pages.
 *
 * - Left: Navigation (sticky)
 * - Center: Main content (children)
 * - Right: Trending/Who to follow (Twitter-style sticky scroll)
 *
 * Sidebars only show for authenticated users on lg+ screens.
 */
export function PulseThreeColumnLayout({
  children,
  hideSidebars = false,
}: PulseThreeColumnLayoutProps): JSX.Element {
  const { user } = useAuth();

  // Twitter-style sticky scroll for right sidebar
  const { ref: stickyRef, style: stickyStyle } = useStickyScroll<HTMLDivElement>({
    topOffset: 80, // Account for header
    bottomOffset: 20,
  });

  const showSidebars = user && !hideSidebars;

  return (
    <div className="flex justify-center">
      {/* Left Navigation */}
      {showSidebars && (
        <aside className="hidden lg:flex w-[320px] flex-shrink-0 justify-end pr-8">
          <div className="w-[240px]">
            <PulseNavigation />
          </div>
        </aside>
      )}

      {/* Main Content */}
      <main className="w-full max-w-[600px] border-x border-border lg:w-[600px]">
        {children}
      </main>

      {/* Right Sidebar with Twitter-style sticky scroll */}
      {showSidebars && (
        <aside className="hidden lg:block w-[320px] flex-shrink-0 pl-8">
          <div
            ref={stickyRef}
            style={stickyStyle}
            className="w-[288px]"
          >
            <PulseRightSidebar />
          </div>
        </aside>
      )}
    </div>
  );
}
