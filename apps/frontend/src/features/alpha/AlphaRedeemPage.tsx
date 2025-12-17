/**
 * Alpha code redemption page
 * Allows authenticated users to redeem their invite code for Nova access
 */

import { useState } from "react";
import { Link, Navigate } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";
import { AuthCard } from "../auth/AuthCard";
import { FormField } from "../../components/ui/FormField";
import { config } from "../../config";
import { getAccessToken } from "../../utils/api";

export function AlphaRedeemPage(): JSX.Element {
  const { user, isAuthenticated, isLoading: authLoading } = useAuth();

  // Form state
  const [code, setCode] = useState("");

  // UI state
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [touched, setTouched] = useState(false);

  // Show loading state while checking authentication
  if (authLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-page">
        <div className="text-text-secondary">Loading...</div>
      </div>
    );
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ returnUrl: "/nova/redeem" }} replace />;
  }

  // Redirect to Nova if user already has access
  if (user?.hasNovaAccess) {
    return <Navigate to="/nova" replace />;
  }

  const validateCode = (value: string): string | undefined => {
    if (!value.trim()) return "Invite code is required";
    return undefined;
  };

  const handleBlur = (): void => {
    setTouched(true);
    const validationError = validateCode(code);
    setError(validationError || null);
  };

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();

    const validationError = validateCode(code);
    setTouched(true);

    if (validationError) {
      setError(validationError);
      return;
    }

    setIsSubmitting(true);
    setError(null);

    try {
      const accessToken = getAccessToken();
      const response = await fetch(`${config.api.baseUrl}/api/alpha/redeem`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({
          code: code.trim().toUpperCase(),
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);

        if (response.status === 400) {
          setError(errorData?.message || "Invalid or already used invite code");
        } else if (response.status === 401) {
          setError("Please log in again and try redeeming your code");
        } else {
          setError(errorData?.message || "Failed to redeem code");
        }
        return;
      }

      // Success! Update user context and redirect to Nova
      // The user will need to refresh their token to get hasNovaAccess updated
      // For now, just redirect and the API will allow access
      window.location.href = "/nova";
    } catch (err) {
      setError(err instanceof Error ? err.message : "An unexpected error occurred");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthCard
      title="Redeem Your Invite Code"
      subtitle="Enter your Nova Alpha invite code to unlock access."
    >
      <div className="mb-6 rounded-lg border border-violet-500/30 bg-violet-500/10 p-4">
        <p className="text-sm text-violet-300">
          Your invite code was sent to your email when your application was approved.
          It looks like: <code className="font-mono text-violet-200">NOVA-XXXXXX</code>
        </p>
      </div>

      {/* Error message */}
      {error && touched && (
        <div className="mb-4 rounded-lg border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-500">
          {error}
        </div>
      )}

      <form className="space-y-4" onSubmit={handleSubmit} noValidate>
        <FormField
          label="Invite Code"
          placeholder="NOVA-XXXXXX"
          type="text"
          value={code}
          onChange={(e) => setCode(e.target.value.toUpperCase())}
          onBlur={handleBlur}
          error={touched && error ? error : undefined}
          disabled={isSubmitting}
          autoComplete="off"
          required
        />

        <button
          type="submit"
          disabled={isSubmitting}
          className="btn-primary w-full rounded-full bg-violet-600 text-white px-4 py-2 text-sm font-medium hover:bg-violet-500 transition-colors disabled:cursor-not-allowed disabled:opacity-50 shadow-lg shadow-violet-600/20"
        >
          {isSubmitting ? "Redeeming..." : "Redeem Code"}
        </button>
      </form>

      <div className="mt-4 flex flex-col gap-2 text-[11px] text-text-secondary">
        <Link
          to="/pulse"
          className="inline-flex items-center gap-1 text-text-tertiary hover:text-text-primary"
        >
          <span>&larr;</span>
          <span>Go to Pulse</span>
        </Link>
        <div>
          Don't have an invite code?{" "}
          <Link to="/alpha/apply" className="text-violet-400 hover:text-violet-300">
            Apply for Alpha access
          </Link>
        </div>
      </div>
    </AuthCard>
  );
}
