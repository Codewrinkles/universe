/**
 * Protected route component
 * Redirects unauthenticated users to login
 */

import { Navigate, useLocation } from "react-router-dom";
import type { ReactNode } from "react";
import { useAuth } from "../../hooks/useAuth";

export interface ProtectedRouteProps {
  children: ReactNode;
  /** Where to redirect unauthenticated users. Defaults to "/login" */
  redirectTo?: string;
}

/**
 * Wraps routes that require authentication
 * Shows loading state while checking auth, redirects if not authenticated
 */
export function ProtectedRoute({ children, redirectTo = "/login" }: ProtectedRouteProps): JSX.Element {
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

  // Redirect if not authenticated
  // Preserve the intended destination for redirect after login (only for /login redirect)
  if (!isAuthenticated) {
    const state = redirectTo === "/login" ? { from: location.pathname } : undefined;
    return <Navigate to={redirectTo} state={state} replace />;
  }

  return <>{children}</>;
}
