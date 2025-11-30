import { Link } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";

interface ErrorPageProps {
  error?: Error;
  resetError?: () => void;
}

export function ErrorPage({ error, resetError }: ErrorPageProps): JSX.Element {
  const { isAuthenticated } = useAuth();
  const homeLink = isAuthenticated ? "/pulse" : "/";

  return (
    <div className="min-h-screen bg-surface-page flex items-center justify-center px-4">
      <div className="text-center max-w-md">
        <h1 className="text-8xl font-bold text-brand-soft mb-4">500</h1>
        <h2 className="text-2xl font-semibold tracking-tight text-text-primary mb-2">
          Something went wrong
        </h2>
        <p className="text-sm text-text-secondary mb-8">
          {error?.message || "An unexpected error occurred. We've been notified and are working on it."}
        </p>
        <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
          {resetError && (
            <button
              onClick={resetError}
              className="inline-flex items-center justify-center rounded-full border border-border bg-surface-card1 text-text-secondary px-6 py-2.5 text-sm font-medium hover:border-brand-soft/60 hover:bg-surface-card2 transition-colors"
            >
              Try again
            </button>
          )}
          <Link
            to={homeLink}
            className="inline-flex items-center justify-center rounded-full bg-brand text-black px-6 py-2.5 text-sm font-medium hover:bg-brand-soft transition-colors"
          >
            Go home
          </Link>
        </div>
      </div>
    </div>
  );
}
