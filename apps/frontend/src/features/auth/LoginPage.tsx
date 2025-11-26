/**
 * User login page
 * Handles email/password login with validation
 */

import { useState } from "react";
import { Link, Navigate, useNavigate, useLocation } from "react-router-dom";
import type { AuthMode, FormErrors } from "../../types";
import { useAuth } from "../../hooks/useAuth";
import { AuthCard } from "./AuthCard";
import { SocialSignInButtons } from "./SocialSignInButtons";
import { FormField } from "../../components/ui/FormField";
import { validateEmail } from "../../utils/validation";
import { ApiError } from "../../utils/api";

export function LoginPage(): JSX.Element {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, isAuthenticated, isLoading: authLoading } = useAuth();

  // Get the redirect destination (if coming from protected route)
  const from = (location.state as { from?: string })?.from || "/";

  // Form state
  const [mode, setMode] = useState<AuthMode>("password");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  // UI state
  const [errors, setErrors] = useState<FormErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [touched, setTouched] = useState<Record<string, boolean>>({});

  // Redirect if already authenticated
  if (authLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-text-secondary">Loading...</div>
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to={from} replace />;
  }

  // Field blur handlers for validation
  const handleBlur = (field: string): void => {
    setTouched((prev) => ({ ...prev, [field]: true }));

    let error: string | undefined;
    if (field === "email") {
      error = validateEmail(email);
    } else if (field === "password" && !password) {
      error = "Password is required";
    }

    setErrors((prev) => ({ ...prev, [field]: error }));
  };

  // Form submission
  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();

    // Validate fields
    const formErrors: FormErrors = {};
    const emailError = validateEmail(email);
    if (emailError) formErrors.email = emailError;
    if (!password) formErrors.password = "Password is required";

    setTouched({ email: true, password: true });
    setErrors(formErrors);

    if (Object.keys(formErrors).length > 0) {
      return;
    }

    setIsSubmitting(true);
    setErrors({});

    try {
      await login({
        email: email.trim(),
        password,
      });

      // Login successful - navigate to original destination or home
      navigate(from, { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.isUnauthorizedError()) {
          setErrors({ general: "Invalid email or password" });
        } else if (error.statusCode === 403) {
          setErrors({ general: "This account has been suspended" });
        } else if (error.statusCode === 423) {
          setErrors({ general: "Account is locked due to too many failed attempts. Please try again later." });
        } else if (error.isValidationError() && error.validationErrors) {
          const serverErrors: FormErrors = {};
          for (const err of error.validationErrors) {
            const field = err.property.toLowerCase() as keyof FormErrors;
            serverErrors[field] = err.message;
          }
          setErrors(serverErrors);
        } else {
          setErrors({ general: error.message });
        }
      } else {
        setErrors({ general: "An unexpected error occurred. Please try again." });
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthCard title="Welcome back" subtitle="Log in to your Codewrinkles account.">
      {/* Mode toggle */}
      <div className="mb-4 inline-flex rounded-full border border-border bg-surface-card2 p-[3px] text-xs">
        <button
          type="button"
          onClick={() => setMode("password")}
          className={`rounded-full px-4 py-1.5 transition-colors ${
            mode === "password"
              ? "bg-surface-page text-text-primary"
              : "text-text-secondary hover:text-text-primary"
          }`}
        >
          Password
        </button>
        <button
          type="button"
          onClick={() => setMode("magic")}
          className={`rounded-full px-4 py-1.5 transition-colors ${
            mode === "magic"
              ? "bg-surface-page text-text-primary"
              : "text-text-secondary hover:text-text-primary"
          }`}
        >
          Magic link
        </button>
      </div>

      {/* General error message */}
      {errors.general && (
        <div className="mb-4 rounded-lg border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-500">
          {errors.general}
        </div>
      )}

      <form className="space-y-4" onSubmit={handleSubmit} noValidate>
        {mode === "password" ? (
          <>
            <FormField
              label="Email"
              placeholder="you@example.com"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onBlur={() => handleBlur("email")}
              error={touched["email"] ? errors.email : undefined}
              disabled={isSubmitting}
              autoComplete="email"
              required
            />

            <FormField
              label="Password"
              placeholder="Enter your password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onBlur={() => handleBlur("password")}
              error={touched["password"] ? errors.password : undefined}
              disabled={isSubmitting}
              autoComplete="current-password"
              required
            />

            <div className="flex items-center justify-between text-xs">
              <label className="flex items-center gap-2 text-text-secondary">
                <input
                  type="checkbox"
                  className="h-3 w-3 rounded border-border bg-surface-page"
                />
                <span>Remember me</span>
              </label>
              <button type="button" className="text-[11px] text-brand-soft hover:text-brand">
                Forgot password?
              </button>
            </div>
          </>
        ) : (
          <>
            <FormField
              label="Email"
              placeholder="you@example.com"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              onBlur={() => handleBlur("email")}
              error={touched["email"] ? errors.email : undefined}
              disabled={isSubmitting}
              autoComplete="email"
              required
            />
            <p className="text-xs text-text-secondary">
              We&apos;ll send you a one-time link to log in.
            </p>
          </>
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          className="btn-primary w-full rounded-full bg-brand-soft px-4 py-2 text-sm font-medium text-black transition-colors hover:bg-brand disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isSubmitting ? "Logging in..." : mode === "password" ? "Continue" : "Send magic link"}
        </button>
      </form>

      <SocialSignInButtons />

      <div className="mt-4 flex flex-col gap-2 text-[11px] text-text-secondary">
        <Link
          to="/"
          className="inline-flex items-center gap-1 text-text-tertiary hover:text-text-primary"
        >
          <span>&larr;</span>
          <span>Back to Home</span>
        </Link>
        <div>
          Don&apos;t have an account?{" "}
          <Link to="/register" className="text-brand-soft hover:text-brand">
            Sign up
          </Link>
        </div>
      </div>
    </AuthCard>
  );
}
