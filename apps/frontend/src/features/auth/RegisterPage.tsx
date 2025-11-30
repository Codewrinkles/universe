/**
 * User registration page
 * Handles email/password registration with validation
 */

import { useState } from "react";
import { Link, Navigate, useNavigate } from "react-router-dom";
import type { FormErrors } from "../../types";
import { useAuth } from "../../hooks/useAuth";
import { AuthCard } from "./AuthCard";
import { SocialSignInButtons } from "./SocialSignInButtons";
import { PasswordStrength } from "./PasswordStrength";
import { FormField } from "../../components/ui/FormField";
import {
  validateEmail,
  validatePassword,
  validateName,
  validateHandle,
  validateRegisterForm,
  hasErrors,
} from "../../utils/validation";
import { ApiError } from "../../utils/api";

export function RegisterPage(): JSX.Element {
  const navigate = useNavigate();
  const { register, isAuthenticated, isLoading: authLoading } = useAuth();

  // Form state
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [handle, setHandle] = useState("");

  // UI state
  const [errors, setErrors] = useState<FormErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  const [justRegistered, setJustRegistered] = useState(false);

  // Show loading state while checking authentication
  if (authLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-text-secondary">Loading...</div>
      </div>
    );
  }

  // Redirect authenticated users away from registration page
  // UNLESS they just registered (in which case we need to navigate them to onboarding)
  if (isAuthenticated && !justRegistered) {
    return <Navigate to="/" replace />;
  }

  // Field blur handlers for validation
  const handleBlur = (field: string): void => {
    setTouched((prev) => ({ ...prev, [field]: true }));

    let error: string | undefined;
    switch (field) {
      case "name":
        error = validateName(name);
        break;
      case "email":
        error = validateEmail(email);
        break;
      case "password":
        error = validatePassword(password);
        break;
      case "handle":
        error = validateHandle(handle);
        break;
    }

    setErrors((prev) => ({ ...prev, [field]: error }));
  };

  // Form submission
  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();

    // Validate all fields
    const formErrors = validateRegisterForm(email, password, name, handle || undefined);

    // Mark all fields as touched
    setTouched({ name: true, email: true, password: true, handle: true });
    setErrors(formErrors);

    if (hasErrors(formErrors)) {
      return;
    }

    setIsSubmitting(true);
    setErrors({});

    try {
      await register({
        email: email.trim(),
        password,
        name: name.trim(),
        handle: handle.trim() || undefined,
      });

      // Mark that we just registered to bypass auth guard
      setJustRegistered(true);

      // Registration successful - navigate to onboarding
      navigate("/onboarding", { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        // Handle validation errors from server
        if (error.isValidationError() && error.validationErrors) {
          const serverErrors: FormErrors = {};
          for (const err of error.validationErrors) {
            const field = err.property.toLowerCase() as keyof FormErrors;
            serverErrors[field] = err.message;
          }
          setErrors(serverErrors);
        } else if (error.isConflictError()) {
          // Duplicate email or handle
          setErrors({ general: error.message });
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
    <AuthCard title="Create your account" subtitle="Join the ecosystem built for value, not engagement.">
      {/* General error message */}
      {errors.general && (
        <div className="mb-4 rounded-lg border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-500">
          {errors.general}
        </div>
      )}

      <form className="space-y-4" onSubmit={handleSubmit} noValidate>
        <FormField
          label="Name"
          placeholder="Daniel"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          onBlur={() => handleBlur("name")}
          error={touched["name"] ? errors.name : undefined}
          disabled={isSubmitting}
          autoComplete="name"
          required
        />

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
          placeholder="Create a password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          onBlur={() => handleBlur("password")}
          error={touched["password"] ? errors.password : undefined}
          disabled={isSubmitting}
          autoComplete="new-password"
          required
        >
          {password && <PasswordStrength password={password} />}
        </FormField>

        <FormField
          label="Handle (optional)"
          placeholder="codewrinkles"
          type="text"
          value={handle}
          onChange={(e) => setHandle(e.target.value)}
          onBlur={() => handleBlur("handle")}
          error={touched["handle"] ? errors.handle : undefined}
          disabled={isSubmitting}
          autoComplete="username"
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
          {isSubmitting ? "Creating account..." : "Create account"}
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
          Already have an account?{" "}
          <Link to="/login" className="text-brand-soft hover:text-brand">
            Log in
          </Link>
        </div>
      </div>
    </AuthCard>
  );
}
