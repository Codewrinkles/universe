/**
 * Nova route component
 * Redirects users without Nova access to /pulse
 */

import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";
import { useAuth } from "../../hooks/useAuth";

export interface NovaRouteProps {
  children: ReactNode;
}

/**
 * Wraps routes that require Nova access
 * Shows loading state while checking auth, redirects to /pulse if no access
 */
export function NovaRoute({ children }: NovaRouteProps): JSX.Element {
  const { user, isAuthenticated, isLoading } = useAuth();

  // Show nothing while checking auth state
  // This prevents flash of redirect for users with access
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-page">
        <div className="text-text-secondary">Loading...</div>
      </div>
    );
  }

  // Redirect to home if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  // Redirect to /nova/redeem if authenticated but no Nova access
  if (!user?.hasNovaAccess) {
    return <Navigate to="/nova/redeem" replace />;
  }

  return <>{children}</>;
}
