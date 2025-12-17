/**
 * User login page
 * Handles email/password login with validation
 */

import { useState } from "react";
import { Link, Navigate, useNavigate, useLocation } from "react-router-dom";
import type { FormErrors } from "../../types";
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
  const from = (location.state as { from?: string })?.from || "/pulse";

  // Form state
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
    <AuthCard title="Welcome back" subtitle="Welcome back to the ecosystem.">
      {/* General error message */}
      {errors.general && (
        <div className="mb-4 rounded-lg border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-500">
          {errors.general}
        </div>
      )}

      <form className="space-y-4" onSubmit={handleSubmit} noValidate>
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

        {/* Legal compliance */}
        <p className="text-[11px] text-text-tertiary text-center">
          By continuing, you agree to our{" "}
          <Link to="/terms" className="text-text-secondary hover:text-text-primary underline">
            Terms of Service
          </Link>{" "}
          and{" "}
          <Link to="/privacy" className="text-text-secondary hover:text-text-primary underline">
            Privacy Policy
          </Link>
          .
        </p>

        <button
          type="submit"
          disabled={isSubmitting}
          className="btn-primary w-full rounded-full bg-brand text-black px-4 py-2 text-sm font-medium hover:bg-brand-soft transition-colors disabled:cursor-not-allowed disabled:opacity-50 shadow-lg shadow-brand/20"
        >
          {isSubmitting ? "Logging in..." : "Continue"}
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
          <Link to="/register" state={{ from }} className="text-brand-soft hover:text-brand">
            Sign up
          </Link>
        </div>
      </div>
    </AuthCard>
  );
}
