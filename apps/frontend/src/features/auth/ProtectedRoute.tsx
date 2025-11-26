/**
 * Protected route component
 * Redirects unauthenticated users to login
 */

import { Navigate, useLocation } from "react-router-dom";
import type { ReactNode } from "react";
import { useAuth } from "../../hooks/useAuth";

export interface ProtectedRouteProps {
  children: ReactNode;
}

/**
 * Wraps routes that require authentication
 * Shows loading state while checking auth, redirects to login if not authenticated
 */
export function ProtectedRoute({ children }: ProtectedRouteProps): JSX.Element {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  // Show nothing while checking auth state
  // This prevents flash of login page for authenticated users
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-page">
        <div className="text-text-secondary">Loading...</div>
      </div>
    );
  }

  // Redirect to login if not authenticated
  // Preserve the intended destination for redirect after login
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  return <>{children}</>;
}
