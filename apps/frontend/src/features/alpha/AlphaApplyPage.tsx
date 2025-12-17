/**
 * Alpha application page
 * Public form for users to apply for Nova Alpha access
 */

import { useState } from "react";
import { Link } from "react-router-dom";
import { Card } from "../../components/ui/Card";
import { FormField } from "../../components/ui/FormField";
import { config } from "../../config";

interface FormErrors {
  email?: string;
  name?: string;
  primaryTechStack?: string;
  yearsOfExperience?: string;
  goal?: string;
  general?: string;
}

export function AlphaApplyPage(): JSX.Element {
  // Form state
  const [email, setEmail] = useState("");
  const [name, setName] = useState("");
  const [primaryTechStack, setPrimaryTechStack] = useState("");
  const [yearsOfExperience, setYearsOfExperience] = useState("");
  const [goal, setGoal] = useState("");

  // UI state
  const [errors, setErrors] = useState<FormErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  const [isSuccess, setIsSuccess] = useState(false);
  const [alreadyApplied, setAlreadyApplied] = useState(false);

  // Validation functions
  const validateEmail = (value: string): string | undefined => {
    if (!value.trim()) return "Email is required";
    if (!value.includes("@") || !value.includes(".")) return "Please enter a valid email";
    if (value.length > 256) return "Email must be 256 characters or less";
    return undefined;
  };

  const validateName = (value: string): string | undefined => {
    if (!value.trim()) return "Name is required";
    if (value.length > 100) return "Name must be 100 characters or less";
    return undefined;
  };

  const validateTechStack = (value: string): string | undefined => {
    if (!value.trim()) return "Primary tech stack is required";
    if (value.length > 200) return "Tech stack must be 200 characters or less";
    return undefined;
  };

  const validateYears = (value: string): string | undefined => {
    if (!value.trim()) return "Years of experience is required";
    const years = parseInt(value, 10);
    if (isNaN(years) || years < 0) return "Please enter a valid number";
    if (years > 50) return "Years of experience seems too high";
    return undefined;
  };

  const validateGoal = (value: string): string | undefined => {
    if (!value.trim()) return "Please tell us your learning goals";
    if (value.length < 20) return "Please provide more details (at least 20 characters)";
    if (value.length > 2000) return "Goal must be 2000 characters or less";
    return undefined;
  };

  // Field blur handlers
  const handleBlur = (field: string): void => {
    setTouched((prev) => ({ ...prev, [field]: true }));

    let error: string | undefined;
    switch (field) {
      case "email":
        error = validateEmail(email);
        break;
      case "name":
        error = validateName(name);
        break;
      case "primaryTechStack":
        error = validateTechStack(primaryTechStack);
        break;
      case "yearsOfExperience":
        error = validateYears(yearsOfExperience);
        break;
      case "goal":
        error = validateGoal(goal);
        break;
    }

    setErrors((prev) => ({ ...prev, [field]: error }));
  };

  // Form submission
  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();

    // Validate all fields
    const formErrors: FormErrors = {
      email: validateEmail(email),
      name: validateName(name),
      primaryTechStack: validateTechStack(primaryTechStack),
      yearsOfExperience: validateYears(yearsOfExperience),
      goal: validateGoal(goal),
    };

    // Mark all fields as touched
    setTouched({
      email: true,
      name: true,
      primaryTechStack: true,
      yearsOfExperience: true,
      goal: true,
    });
    setErrors(formErrors);

    // Check for errors
    if (Object.values(formErrors).some((e) => e !== undefined)) {
      return;
    }

    setIsSubmitting(true);
    setErrors({});

    try {
      const response = await fetch(`${config.api.baseUrl}/api/alpha/apply`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email: email.trim(),
          name: name.trim(),
          primaryTechStack: primaryTechStack.trim(),
          yearsOfExperience: parseInt(yearsOfExperience, 10),
          goal: goal.trim(),
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.message || "Failed to submit application");
      }

      const data = await response.json();
      setIsSuccess(true);
      setAlreadyApplied(data.alreadyApplied);
    } catch (error) {
      setErrors({
        general: error instanceof Error ? error.message : "An unexpected error occurred",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  // Success state
  if (isSuccess) {
    return (
      <div className="min-h-screen bg-surface-page flex items-center justify-center p-4">
        <Card className="relative overflow-hidden w-full max-w-lg">
          <div className="absolute inset-x-8 top-0 h-px bg-gradient-to-r from-transparent via-violet-500/60 to-transparent opacity-60" />
          <div className="mb-4">
            <h1 className="text-base font-semibold tracking-tight text-text-primary">
              {alreadyApplied ? "Already Applied!" : "Application Received!"}
            </h1>
            <p className="mt-1 text-xs text-text-secondary">
              {alreadyApplied
                ? "You've already submitted an application. We'll be in touch soon!"
                : "Thanks for applying to Nova Alpha. We'll review your application and get back to you within 48 hours."}
            </p>
          </div>
          <div className="space-y-4">
            <div className="rounded-lg border border-brand/30 bg-brand/10 px-4 py-3 text-sm text-brand">
              Check your email for updates on your application status.
            </div>

            <div className="flex flex-col gap-2 text-sm">
              <Link to="/" className="text-text-secondary hover:text-text-primary">
                &larr; Back to Home
              </Link>
              <Link to="/pulse" className="text-brand-soft hover:text-brand">
                Explore Pulse while you wait &rarr;
              </Link>
            </div>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-surface-page flex items-center justify-center p-4">
      <Card className="relative overflow-hidden w-full max-w-3xl">
        <div className="absolute inset-x-8 top-0 h-px bg-gradient-to-r from-transparent via-violet-500/60 to-transparent opacity-60" />

        {/* Header */}
        <div className="mb-6">
          <h1 className="text-lg font-semibold tracking-tight text-text-primary">Apply for Codewrinkles Nova Alpha Testing NOW!</h1>
          <p className="mt-1 text-sm text-text-secondary">Be one of <span className="text-violet-300 font-medium">ONLY 50</span> people who will get access.</p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Left column - Benefits (1/3 width on desktop) */}
          <div className="lg:col-span-1 space-y-4">
            {/* Urgency badge */}
            <div className="flex items-center gap-2 text-xs">
              <span className="inline-flex items-center gap-1.5 rounded-full bg-violet-500/20 border border-violet-500/30 px-3 py-1 text-violet-300 font-medium">
                <span className="relative flex h-2 w-2">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-violet-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-2 w-2 bg-violet-500"></span>
                </span>
                Limited to 50 spots
              </span>
            </div>

            {/* What you get - Premium feel with gradient */}
            <div className="relative rounded-xl border border-violet-500/30 bg-gradient-to-br from-violet-500/10 via-violet-600/5 to-transparent p-4 overflow-hidden">
              <div className="absolute top-0 right-0 w-20 h-20 bg-violet-500/10 rounded-full blur-2xl -mr-10 -mt-10"></div>
              <h3 className="text-sm font-semibold text-violet-300 mb-3 flex items-center gap-2">
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v13m0-13V6a2 2 0 112 2h-2zm0 0V5.5A2.5 2.5 0 109.5 8H12zm-7 4h14M5 12a2 2 0 110-4h14a2 2 0 110 4M5 12v7a2 2 0 002 2h10a2 2 0 002-2v-7" />
                </svg>
                What you get
              </h3>
              <ul className="space-y-2.5">
                <li className="flex items-start gap-2 text-xs text-text-primary">
                  <svg className="w-4 h-4 text-emerald-400 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                  <span><strong className="text-violet-200">Free unlimited</strong> access during Alpha</span>
                </li>
                <li className="flex items-start gap-2 text-xs text-text-primary">
                  <svg className="w-4 h-4 text-emerald-400 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                  <span><strong className="text-violet-200">Direct influence</strong> on features we build</span>
                </li>
                <li className="flex items-start gap-2 text-xs text-text-primary">
                  <svg className="w-4 h-4 text-emerald-400 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                  <span><strong className="text-violet-200">Founding member</strong> status forever</span>
                </li>
                <li className="flex items-start gap-2 text-xs text-text-primary">
                  <svg className="w-4 h-4 text-emerald-400 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                  <span><strong className="text-violet-200">Priority Beta</strong> access + lifetime discount</span>
                </li>
              </ul>
            </div>

            {/* What we ask - Lighter, more subtle */}
            <div className="rounded-xl border border-border bg-surface-card2/50 p-4">
              <h3 className="text-sm font-medium text-text-secondary mb-3 flex items-center gap-2">
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                In return, we ask
              </h3>
              <ul className="space-y-2">
                <li className="flex items-start gap-2 text-xs text-text-tertiary">
                  <span className="text-text-tertiary shrink-0">→</span>
                  <span>Complete your learning profile within 24h</span>
                </li>
                <li className="flex items-start gap-2 text-xs text-text-tertiary">
                  <span className="text-text-tertiary shrink-0">→</span>
                  <span>Have at least 5 conversations in 2 weeks</span>
                </li>
                <li className="flex items-start gap-2 text-xs text-text-tertiary">
                  <span className="text-text-tertiary shrink-0">→</span>
                  <span>Share honest feedback (quick survey)</span>
                </li>
              </ul>
            </div>
          </div>

          {/* Right column - Form (2/3 width on desktop) */}
          <div className="lg:col-span-2">
            {/* General error message */}
            {errors.general && (
              <div className="mb-4 rounded-lg border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-500">
                {errors.general}
              </div>
            )}

            <form className="space-y-4" onSubmit={handleSubmit} noValidate>
              {/* Two-column row for Email and Name on desktop */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
                  label="Name"
                  placeholder="Your name"
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  onBlur={() => handleBlur("name")}
                  error={touched["name"] ? errors.name : undefined}
                  disabled={isSubmitting}
                  autoComplete="name"
                  required
                />
              </div>

              {/* Two-column row for Tech Stack and Years on desktop */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <FormField
                  label="Primary Tech Stack"
                  placeholder="e.g., .NET, React, Python"
                  type="text"
                  value={primaryTechStack}
                  onChange={(e) => setPrimaryTechStack(e.target.value)}
                  onBlur={() => handleBlur("primaryTechStack")}
                  error={touched["primaryTechStack"] ? errors.primaryTechStack : undefined}
                  disabled={isSubmitting}
                  required
                />

                <FormField
                  label="Years of Experience"
                  placeholder="Years in software development"
                  type="number"
                  value={yearsOfExperience}
                  onChange={(e) => setYearsOfExperience(e.target.value)}
                  onBlur={() => handleBlur("yearsOfExperience")}
                  error={touched["yearsOfExperience"] ? errors.yearsOfExperience : undefined}
                  disabled={isSubmitting}
                  required
                />
              </div>

              {/* Full width textarea */}
              <div className="space-y-1">
                <label className="block text-xs font-medium text-text-secondary">
                  What do you hope to achieve with Nova? <span className="text-red-500">*</span>
                </label>
                <textarea
                  placeholder="Tell us about your learning goals..."
                  value={goal}
                  onChange={(e) => setGoal(e.target.value)}
                  onBlur={() => handleBlur("goal")}
                  disabled={isSubmitting}
                  rows={4}
                  className={`w-full rounded-lg border bg-surface-card1 px-3 py-2 text-sm text-text-primary placeholder-text-tertiary focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand disabled:cursor-not-allowed disabled:opacity-50 ${
                    touched["goal"] && errors.goal ? "border-red-500" : "border-border"
                  }`}
                />
                {touched["goal"] && errors.goal && (
                  <p className="text-xs text-red-500">{errors.goal}</p>
                )}
                <p className="text-xs text-text-tertiary">{goal.length}/2000 characters</p>
              </div>

              <button
                type="submit"
                disabled={isSubmitting}
                className="w-full rounded-full bg-violet-600 text-white px-4 py-2 text-sm font-medium hover:bg-violet-500 transition-colors disabled:cursor-not-allowed disabled:opacity-50 shadow-lg shadow-violet-600/20"
              >
                {isSubmitting ? "Submitting..." : "Submit Application"}
              </button>
            </form>

            <div className="mt-4 flex flex-col gap-2 text-[11px] text-text-secondary">
              <Link
                to="/"
                className="inline-flex items-center gap-1 text-text-tertiary hover:text-text-primary"
              >
                <span>&larr;</span>
                <span>Back to Home</span>
              </Link>
              <div>
                Already have an invite code?{" "}
                <Link to="/nova/redeem" className="text-violet-400 hover:text-violet-300">
                  Redeem it here
                </Link>
              </div>
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}
