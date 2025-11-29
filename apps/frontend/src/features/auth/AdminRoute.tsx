/**
 * Admin route component
 * Redirects non-admin users to /pulse
 */

import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";
import { useAuth } from "../../hooks/useAuth";

export interface AdminRouteProps {
  children: ReactNode;
}

/**
 * Wraps routes that require admin role
 * Shows loading state while checking auth, redirects to /pulse if not admin
 */
export function AdminRoute({ children }: AdminRouteProps): JSX.Element {
  const { user, isAuthenticated, isLoading } = useAuth();

  // Show nothing while checking auth state
  // This prevents flash of redirect for admin users
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-page">
        <div className="text-text-secondary">Loading...</div>
      </div>
    );
  }

  // Redirect to /pulse if not authenticated or not admin
  if (!isAuthenticated || user?.role !== "Admin") {
    return <Navigate to="/pulse" replace />;
  }

  return <>{children}</>;
}
