import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";

export function OAuthSuccessPage(): JSX.Element {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { completeOAuthLogin } = useAuth();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const handleSuccess = async (): Promise<void> => {
      try {
        const accessToken = searchParams.get("access_token");
        const refreshToken = searchParams.get("refresh_token");
        const isNewUser = searchParams.get("is_new_user") === "true";

        if (!accessToken || !refreshToken) {
          setError("Missing authentication tokens");
          setTimeout(() => navigate("/login"), 3000);
          return;
        }

        await completeOAuthLogin(accessToken, refreshToken);

        window.history.replaceState({}, document.title, "/auth/success");

        if (isNewUser) {
          navigate("/onboarding");
        } else {
          navigate("/pulse");
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to complete authentication");
        setTimeout(() => navigate("/login"), 3000);
      }
    };

    handleSuccess();
  }, [searchParams, navigate, completeOAuthLogin]);

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-page">
        <div className="text-center">
          <div className="mb-4 text-red-400">{error}</div>
          <div className="text-sm text-text-secondary">Redirecting to login...</div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-page">
      <div className="text-center">
        <div className="mb-4 text-text-primary">Completing sign-in...</div>
        <div className="text-sm text-text-secondary">Please wait...</div>
      </div>
    </div>
  );
}
