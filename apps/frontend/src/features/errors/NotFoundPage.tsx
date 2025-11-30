import { Link } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";

export function NotFoundPage(): JSX.Element {
  const { isAuthenticated } = useAuth();
  const homeLink = isAuthenticated ? "/pulse" : "/";

  return (
    <div className="min-h-screen bg-surface-page flex items-center justify-center px-4">
      <div className="text-center max-w-md">
        <h1 className="text-8xl font-bold text-brand-soft mb-4">404</h1>
        <h2 className="text-2xl font-semibold tracking-tight text-text-primary mb-2">
          Page not found
        </h2>
        <p className="text-sm text-text-secondary mb-8">
          The page you're looking for doesn't exist or has been moved.
        </p>
        <Link
          to={homeLink}
          className="inline-flex items-center justify-center rounded-full bg-brand text-black px-6 py-2.5 text-sm font-medium hover:bg-brand-soft transition-colors"
        >
          Go home
        </Link>
      </div>
    </div>
  );
}
