import { useState } from "react";
import { Link } from "react-router-dom";
import type { AuthMode } from "../../types";
import { AuthCard } from "./AuthCard";
import { SocialSignInButtons } from "./SocialSignInButtons";

function Field({ label, placeholder, type = "text" }: { label: string; placeholder: string; type?: string }): JSX.Element {
  return (
    <div className="space-y-1">
      <label className="block text-xs text-text-tertiary">{label}</label>
      <input
        type={type}
        placeholder={placeholder}
        className="w-full rounded-xl border border-border bg-surface-page px-3 py-2 text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-brand-soft/70 focus:ring-offset-2 focus:ring-offset-surface-page"
      />
    </div>
  );
}

export function LoginPage(): JSX.Element {
  const [mode, setMode] = useState<AuthMode>("password");

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();
    // Placeholder for future backend integration
  };

  return (
    <AuthCard title="Welcome back" subtitle="Log in to your Codewrinkles account.">
      {/* Mode toggle */}
      <div className="inline-flex rounded-full border border-border bg-surface-card2 p-[3px] text-xs mb-4">
        <button
          type="button"
          onClick={() => setMode("password")}
          className={`px-4 py-1.5 rounded-full transition-colors ${
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
          className={`px-4 py-1.5 rounded-full transition-colors ${
            mode === "magic"
              ? "bg-surface-page text-text-primary"
              : "text-text-secondary hover:text-text-primary"
          }`}
        >
          Magic link
        </button>
      </div>

      <form className="space-y-4" onSubmit={handleSubmit}>
        {mode === "password" ? (
          <>
            <Field label="Email or username" placeholder="you@example.com" />
            <Field label="Password" placeholder="••••••••" type="password" />
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
            <Field label="Email" placeholder="you@example.com" />
            <p className="text-xs text-text-secondary">
              We&apos;ll send you a one-time link to log in.
            </p>
          </>
        )}

        <button
          type="submit"
          className="btn-primary w-full rounded-full bg-brand-soft px-4 py-2 text-sm font-medium text-black hover:bg-brand transition-colors"
        >
          {mode === "password" ? "Continue" : "Send magic link"}
        </button>
      </form>

      <SocialSignInButtons />

      <div className="mt-4 flex flex-col gap-2 text-[11px] text-text-secondary">
        <Link
          to="/"
          className="inline-flex items-center gap-1 text-text-tertiary hover:text-text-primary"
        >
          <span>←</span>
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
