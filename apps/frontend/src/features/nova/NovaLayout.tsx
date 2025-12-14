import { useState } from "react";
import { Outlet } from "react-router-dom";
import { NovaSidebar } from "./NovaSidebar";

/**
 * NovaLayout - Two-panel layout for the Nova AI learning coach
 *
 * Structure:
 * - Left: Sidebar with conversations and learning paths (280px, fixed)
 * - Right: Router Outlet (scrolls independently)
 *
 * Uses a fixed viewport layout where both panels have constrained height
 * and scroll independently (like a chat application).
 */
export function NovaLayout(): JSX.Element {
  const [isMobileSidebarOpen, setIsMobileSidebarOpen] = useState(false);

  return (
    <div className="flex h-[calc(100vh-4rem)]">
      {/* Mobile overlay */}
      {isMobileSidebarOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-40 lg:hidden"
          onClick={() => setIsMobileSidebarOpen(false)}
        />
      )}

      {/* Left Sidebar - fixed height, scrolls independently */}
      <aside
        className={`
          fixed lg:relative top-16 lg:top-0 left-0 h-[calc(100vh-4rem)] z-50 lg:z-0
          w-[280px] flex-shrink-0 overflow-y-auto custom-scrollbar
          transform transition-transform duration-300 ease-in-out
          ${isMobileSidebarOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"}
        `}
      >
        <NovaSidebar onMobileClose={() => setIsMobileSidebarOpen(false)} />
      </aside>

      {/* Main Content Area - fixed height, scrolls independently */}
      <main className="flex-1 h-full overflow-y-auto custom-scrollbar border-l border-border">
        <Outlet />
      </main>

      {/* Mobile menu button */}
      <button
        type="button"
        onClick={() => setIsMobileSidebarOpen(true)}
        className="fixed bottom-6 left-6 z-40 lg:hidden flex items-center justify-center w-12 h-12 rounded-full bg-violet-500 text-white shadow-lg hover:bg-violet-400 transition-all"
        aria-label="Open sidebar"
      >
        <svg
          className="w-5 h-5"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M4 6h16M4 12h16M4 18h16"
          />
        </svg>
      </button>
    </div>
  );
}
