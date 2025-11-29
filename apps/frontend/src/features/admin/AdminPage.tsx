import { useState } from "react";
import { Outlet } from "react-router-dom";
import { AdminNavigation } from "./AdminNavigation";

/**
 * AdminPage layout with navigation sidebar and content area.
 * Uses React Router's Outlet for nested admin routes.
 */
export function AdminPage(): JSX.Element {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  return (
    <div className="mx-auto flex max-w-full relative">
      {/* Mobile menu button - Floating Action Button */}
      <button
        type="button"
        onClick={() => setIsMobileMenuOpen(true)}
        className="fixed bottom-6 right-6 z-40 md:hidden flex items-center justify-center w-14 h-14 rounded-full bg-brand-soft border-2 border-brand text-black shadow-2xl hover:bg-brand transition-all hover:scale-110"
        aria-label="Open menu"
      >
        <span className="text-2xl font-bold">☰</span>
      </button>

      {/* Mobile overlay */}
      {isMobileMenuOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-40 md:hidden"
          onClick={() => setIsMobileMenuOpen(false)}
        />
      )}

      {/* Left sidebar - Navigation */}
      <div
        className={`
          fixed md:sticky top-0 left-0 h-screen z-50 md:z-0
          w-64 md:w-48 flex-shrink-0
          transform transition-transform duration-300 ease-in-out
          ${isMobileMenuOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'}
        `}
      >
        <AdminNavigation onMobileClose={() => setIsMobileMenuOpen(false)} />
      </div>

      {/* Main content area */}
      <div className="flex-1 border-l border-border min-h-screen">
        <Outlet />
      </div>
    </div>
  );
}
