import { useEffect } from "react";
import { Link, useSearchParams } from "react-router-dom";

export function OAuthErrorPage(): JSX.Element {
  const [searchParams] = useSearchParams();
  const message = searchParams.get("message") || "An error occurred during authentication";

  useEffect(() => {
    window.history.replaceState({}, document.title, "/auth/error");
  }, []);

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-page">
      <div className="max-w-md text-center">
        <div className="mb-6 text-4xl">⚠️</div>
        <h1 className="mb-4 text-xl font-semibold text-text-primary">
          Authentication Failed
        </h1>
        <p className="mb-6 text-sm text-text-secondary">{message}</p>
        <Link
          to="/login"
          className="inline-flex items-center justify-center rounded-full bg-brand text-black px-6 py-2 text-sm font-medium hover:bg-brand-soft transition-colors"
        >
          Back to Login
        </Link>
      </div>
    </div>
  );
}
