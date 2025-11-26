/**
 * User registration page
 * Handles email/password registration with validation
 */

import { useState } from "react";
import { Link, Navigate, useNavigate } from "react-router-dom";
import type { AuthMode, FormErrors } from "../../types";
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
  const [mode, setMode] = useState<AuthMode>("password");
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [handle, setHandle] = useState("");

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

      // Registration successful - navigate to home
      navigate("/", { replace: true });
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
    <AuthCard title="Create your account" subtitle="Start your Codewrinkles workspace.">
      {/* Auth mode toggle */}
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

        {mode === "password" && (
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
        )}

        {mode === "password" && (
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
        )}

        {mode === "magic" && (
          <p className="text-xs text-text-secondary">
            We&apos;ll create your account after you confirm the magic link.
          </p>
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          className="btn-primary w-full rounded-full bg-brand-soft px-4 py-2 text-sm font-medium text-black transition-colors hover:bg-brand disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isSubmitting ? "Creating account..." : mode === "password" ? "Create account" : "Send magic link"}
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
