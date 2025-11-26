import { useState } from "react";
import { useNavigate } from "react-router-dom";
import type { OnboardingStep } from "../../types";
import { Card } from "../../components/ui/Card";

const ONBOARDING_STEPS: OnboardingStep[] = [
  {
    id: 1,
    title: "What do you want Codewrinkles for?",
    description: "We'll tune the surfaces around your primary focus.",
    chips: ["Social posting", "Learning", "Twin Q&A", "Running & training"],
  },
  {
    id: 2,
    title: "Which apps should we start with?",
    description: "You can always enable more later in Settings → Connected apps.",
    chips: ["Social", "Learn", "Twin", "Legal (soon)", "Runwrinkles (soon)"],
  },
  {
    id: 3,
    title: "Set your initial rhythm",
    description: "How often do you want Codewrinkles to nudge you?",
    chips: ["Daily check-in", "3x per week", "Weekly recap only", "No nudges for now"],
  },
];

export function OnboardingPage(): JSX.Element {
  const navigate = useNavigate();
  const [stepIndex, setStepIndex] = useState(0);
  const step = ONBOARDING_STEPS[stepIndex];

  if (!step) {
    // Safety check - should never happen with valid stepIndex
    return <div>Error: Invalid step</div>;
  }

  const isLast = stepIndex === ONBOARDING_STEPS.length - 1;

  const goNext = (): void => {
    if (!isLast) {
      setStepIndex(stepIndex + 1);
    } else {
      navigate("/");
    }
  };

  const goBack = (): void => {
    if (stepIndex === 0) {
      navigate("/");
    } else {
      setStepIndex(stepIndex - 1);
    }
  };

  const skip = (): void => {
    navigate("/");
  };

  return (
    <div className="min-h-[60vh] flex items-center justify-center px-4 py-6 lg:py-8">
      <div className="w-full max-w-xl">
        <Card>
          <div className="flex items-center justify-between mb-4">
            <div>
              <h1 className="text-base font-semibold tracking-tight text-text-primary">
                Quick onboarding
              </h1>
              <p className="mt-1 text-xs text-text-secondary">
                3 small steps. You can always change this later.
              </p>
            </div>
            <div className="text-[11px] text-text-tertiary">
              Step {stepIndex + 1} of {ONBOARDING_STEPS.length}
            </div>
          </div>

          <div className="mb-4">
            <h2 className="text-sm font-semibold tracking-tight text-text-primary">
              {step.title}
            </h2>
            <p className="mt-1 text-xs text-text-secondary">{step.description}</p>
          </div>

          <div className="mb-5 flex flex-wrap gap-2">
            {step.chips.map((chip) => (
              <button
                key={chip}
                type="button"
                className="rounded-full border border-border bg-surface-card2 px-3 py-1.5 text-xs text-text-secondary hover:border-brand-soft/60 hover:bg-surface-page transition-colors"
              >
                {chip}
              </button>
            ))}
          </div>

          <div className="flex items-center justify-between text-xs">
            <button
              type="button"
              onClick={goBack}
              className="inline-flex items-center gap-1 text-text-tertiary hover:text-text-primary"
            >
              <span>←</span>
              <span>{stepIndex === 0 ? "Back to app" : "Back"}</span>
            </button>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={skip}
                className="text-text-tertiary hover:text-text-primary"
              >
                Skip
              </button>
              <button
                type="button"
                onClick={goNext}
                className="btn-primary inline-flex items-center rounded-full bg-brand-soft px-4 py-1.5 text-xs font-medium text-black hover:bg-brand"
              >
                {isLast ? "Finish" : "Next"}
              </button>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}
