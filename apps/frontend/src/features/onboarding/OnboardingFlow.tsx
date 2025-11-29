import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { CompleteProfile } from "./steps/CompleteProfile";
import { FirstPulse } from "./steps/FirstPulse";
import { SuggestedFollows } from "./steps/SuggestedFollows";
import { config } from "../../config";
import type { OnboardingStatus } from "../../types";

type OnboardingStep = "profile" | "pulse" | "follows";

export function OnboardingFlow(): JSX.Element {
  const navigate = useNavigate();
  const [status, setStatus] = useState<OnboardingStatus | null>(null);
  const [currentStep, setCurrentStep] = useState<OnboardingStep>("profile");
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function fetchStatus(): Promise<void> {
      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        const response = await fetch(`${config.api.baseUrl}/api/identity/onboarding/status`, {
          headers: {
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
        });

        if (!response.ok) {
          throw new Error("Failed to fetch onboarding status");
        }

        const data: OnboardingStatus = await response.json();
        setStatus(data);

        // Redirect if already completed
        if (data.isCompleted) {
          navigate("/pulse");
          return;
        }

        // Determine starting step
        if (!data.hasHandle || !data.hasBio) {
          setCurrentStep("profile");
        } else if (!data.hasPostedPulse) {
          setCurrentStep("pulse");
        } else {
          setCurrentStep("follows");
        }
      } catch (error) {
        console.error("Failed to fetch onboarding status:", error);
      } finally {
        setIsLoading(false);
      }
    }

    void fetchStatus();
  }, [navigate]);

  const handleProfileComplete = (): void => {
    setCurrentStep("pulse");
  };

  const handlePulseComplete = (): void => {
    setCurrentStep("follows");
  };

  const handleFollowsComplete = async (): Promise<void> => {
    try {
      const token = localStorage.getItem(config.auth.accessTokenKey);
      await fetch(`${config.api.baseUrl}/api/identity/onboarding/complete`, {
        method: "POST",
        headers: {
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
      });
      navigate("/pulse");
    } catch (error) {
      console.error("Failed to complete onboarding:", error);
      // Navigate anyway
      navigate("/pulse");
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-surface-page flex items-center justify-center">
        <div className="text-text-secondary">Loading...</div>
      </div>
    );
  }

  if (!status) {
    return (
      <div className="min-h-screen bg-surface-page flex items-center justify-center">
        <div className="text-text-secondary">Failed to load onboarding</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-surface-page flex items-center justify-center p-4">
      <div className="max-w-2xl w-full">
        {/* Progress Indicator */}
        <div className="mb-8">
          <div className="flex items-center justify-between mb-2">
            <div className={`flex items-center ${currentStep === "profile" ? "text-brand" : "text-text-tertiary"}`}>
              <span className="text-sm font-medium">1. Profile</span>
            </div>
            <div className={`flex items-center ${currentStep === "pulse" ? "text-brand" : "text-text-tertiary"}`}>
              <span className="text-sm font-medium">2. First Pulse</span>
            </div>
            <div className={`flex items-center ${currentStep === "follows" ? "text-brand" : "text-text-tertiary"}`}>
              <span className="text-sm font-medium">3. Follow People</span>
            </div>
          </div>
          <div className="h-1 bg-surface-card1 rounded-full overflow-hidden">
            <div
              className="h-full bg-brand transition-all duration-300"
              style={{
                width: currentStep === "profile" ? "33%" : currentStep === "pulse" ? "66%" : "100%"
              }}
            />
          </div>
        </div>

        {/* Steps */}
        {currentStep === "profile" && <CompleteProfile onComplete={handleProfileComplete} />}
        {currentStep === "pulse" && <FirstPulse onComplete={handlePulseComplete} />}
        {currentStep === "follows" && <SuggestedFollows onComplete={handleFollowsComplete} />}
      </div>
    </div>
  );
}
