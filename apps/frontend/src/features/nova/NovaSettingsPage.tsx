import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { useLearnerProfile } from "./hooks/useLearnerProfile";
import type { LearningStyle, PreferredPace } from "./types";

interface FormData {
  currentRole: string;
  experienceYears: string;
  primaryTechStack: string;
  learningGoals: string;
  learningStyle: LearningStyle | "";
  preferredPace: PreferredPace | "";
}

const LEARNING_STYLE_OPTIONS: { value: LearningStyle; label: string; description: string }[] = [
  {
    value: "ExamplesFirst",
    label: "Examples first",
    description: "Show me code, then explain",
  },
  {
    value: "TheoryFirst",
    label: "Theory first",
    description: "Concepts first, then examples",
  },
  {
    value: "HandsOn",
    label: "Hands-on",
    description: "Let me try, then explain",
  },
];

const PACE_OPTIONS: { value: PreferredPace; label: string; description: string }[] = [
  {
    value: "QuickOverview",
    label: "Quick",
    description: "Just the essentials",
  },
  {
    value: "Balanced",
    label: "Balanced",
    description: "Moderate depth",
  },
  {
    value: "DeepDive",
    label: "Deep dive",
    description: "Cover edge cases",
  },
];

export function NovaSettingsPage(): JSX.Element {
  const { profile, isLoading, updateProfile, isUpdating, error } = useLearnerProfile();
  const [formData, setFormData] = useState<FormData>({
    currentRole: "",
    experienceYears: "",
    primaryTechStack: "",
    learningGoals: "",
    learningStyle: "",
    preferredPace: "",
  });
  const [saveSuccess, setSaveSuccess] = useState(false);

  // Populate form when profile loads
  useEffect(() => {
    if (profile) {
      setFormData({
        currentRole: profile.currentRole || "",
        experienceYears: profile.experienceYears?.toString() || "",
        primaryTechStack: profile.primaryTechStack || "",
        learningGoals: profile.learningGoals || "",
        learningStyle: profile.learningStyle || "",
        preferredPace: profile.preferredPace || "",
      });
    }
  }, [profile]);

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    setSaveSuccess(false);

    try {
      await updateProfile({
        currentRole: formData.currentRole || null,
        experienceYears: formData.experienceYears ? parseInt(formData.experienceYears, 10) : null,
        primaryTechStack: formData.primaryTechStack || null,
        learningGoals: formData.learningGoals || null,
        learningStyle: formData.learningStyle || null,
        preferredPace: formData.preferredPace || null,
      });
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch {
      // Error is handled by the hook
    }
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
  ): void => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="w-8 h-8 border-2 border-violet-500/30 border-t-violet-500 rounded-full animate-spin" />
      </div>
    );
  }

  return (
    <div className="h-full overflow-y-auto">
      <div className="max-w-5xl mx-auto p-6">
        {/* Header */}
        <div className="mb-6">
          <div className="flex items-center gap-3">
            <Link
              to="/nova/c/new"
              className="p-2 rounded-lg text-text-secondary hover:text-text-primary hover:bg-surface-card1 transition-colors"
              aria-label="Back to chat"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
            </Link>
            <div>
              <h1 className="text-lg font-semibold text-text-primary">Your Learning Profile</h1>
              <p className="text-sm text-text-secondary">
                Help Cody understand your background and how you learn best
              </p>
            </div>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Professional Background - Compact Grid */}
          <section className="p-4 rounded-xl bg-surface-card1 border border-border">
            <h2 className="text-xs font-semibold text-text-tertiary uppercase tracking-wide mb-4">
              Professional Background
            </h2>

            <div className="grid gap-4 sm:grid-cols-3">
              <div>
                <label htmlFor="currentRole" className="block text-sm text-text-secondary mb-1.5">
                  Current Role
                </label>
                <input
                  type="text"
                  id="currentRole"
                  name="currentRole"
                  value={formData.currentRole}
                  onChange={handleChange}
                  placeholder="e.g., Scrum Master"
                  className="w-full px-3 py-2 rounded-lg bg-surface-page border border-border text-text-primary text-sm placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-violet-500/50"
                />
              </div>

              <div>
                <label htmlFor="experienceYears" className="block text-sm text-text-secondary mb-1.5">
                  Years Experience
                </label>
                <input
                  type="number"
                  id="experienceYears"
                  name="experienceYears"
                  value={formData.experienceYears}
                  onChange={handleChange}
                  min="0"
                  max="50"
                  placeholder="e.g., 5"
                  className="w-full px-3 py-2 rounded-lg bg-surface-page border border-border text-text-primary text-sm placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-violet-500/50"
                />
              </div>

              <div>
                <label htmlFor="primaryTechStack" className="block text-sm text-text-secondary mb-1.5">
                  Primary Tech Stack
                </label>
                <input
                  type="text"
                  id="primaryTechStack"
                  name="primaryTechStack"
                  value={formData.primaryTechStack}
                  onChange={handleChange}
                  placeholder="e.g., Python, Django"
                  className="w-full px-3 py-2 rounded-lg bg-surface-page border border-border text-text-primary text-sm placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-violet-500/50"
                />
              </div>
            </div>
          </section>

          {/* Two Column Layout: Goals + Preferences */}
          <div className="grid gap-6 lg:grid-cols-2">
            {/* Learning Goals */}
            <section className="p-4 rounded-xl bg-surface-card1 border border-border">
              <h2 className="text-xs font-semibold text-text-tertiary uppercase tracking-wide mb-4">
                Learning Goals
              </h2>
              <textarea
                id="learningGoals"
                name="learningGoals"
                value={formData.learningGoals}
                onChange={handleChange}
                rows={4}
                placeholder="What do you want to learn? e.g., Transition to .NET, learn Clean Architecture and CQRS patterns"
                className="w-full px-3 py-2 rounded-lg bg-surface-page border border-border text-text-primary text-sm placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-violet-500/50 resize-none"
              />
            </section>

            {/* Learning Preferences */}
            <section className="p-4 rounded-xl bg-surface-card1 border border-border">
              <h2 className="text-xs font-semibold text-text-tertiary uppercase tracking-wide mb-4">
                How You Learn Best
              </h2>

              <div className="grid gap-4 sm:grid-cols-2">
                {/* Learning Style */}
                <div>
                  <label className="block text-sm text-text-secondary mb-2">Learning Style</label>
                  <div className="space-y-1.5">
                    {LEARNING_STYLE_OPTIONS.map((option) => (
                      <label
                        key={option.value}
                        className={`flex items-center gap-2 px-3 py-2 rounded-lg border cursor-pointer transition-colors ${
                          formData.learningStyle === option.value
                            ? "bg-violet-500/10 border-violet-500/40"
                            : "bg-surface-page border-border hover:border-violet-500/30"
                        }`}
                      >
                        <input
                          type="radio"
                          name="learningStyle"
                          value={option.value}
                          checked={formData.learningStyle === option.value}
                          onChange={handleChange}
                          className="accent-violet-500"
                        />
                        <div className="min-w-0">
                          <span className="text-sm font-medium text-text-primary">{option.label}</span>
                          <span className="text-xs text-text-tertiary ml-1.5">{option.description}</span>
                        </div>
                      </label>
                    ))}
                  </div>
                </div>

                {/* Preferred Pace */}
                <div>
                  <label className="block text-sm text-text-secondary mb-2">Explanation Depth</label>
                  <div className="space-y-1.5">
                    {PACE_OPTIONS.map((option) => (
                      <label
                        key={option.value}
                        className={`flex items-center gap-2 px-3 py-2 rounded-lg border cursor-pointer transition-colors ${
                          formData.preferredPace === option.value
                            ? "bg-violet-500/10 border-violet-500/40"
                            : "bg-surface-page border-border hover:border-violet-500/30"
                        }`}
                      >
                        <input
                          type="radio"
                          name="preferredPace"
                          value={option.value}
                          checked={formData.preferredPace === option.value}
                          onChange={handleChange}
                          className="accent-violet-500"
                        />
                        <div className="min-w-0">
                          <span className="text-sm font-medium text-text-primary">{option.label}</span>
                          <span className="text-xs text-text-tertiary ml-1.5">{option.description}</span>
                        </div>
                      </label>
                    ))}
                  </div>
                </div>
              </div>
            </section>
          </div>

          {/* Error and Success Messages */}
          {error && (
            <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
              {error}
            </div>
          )}

          {saveSuccess && (
            <div className="p-3 rounded-xl bg-green-500/10 border border-green-500/30 text-green-400 text-sm">
              Profile saved successfully!
            </div>
          )}

          {/* Submit Button */}
          <div className="flex justify-end gap-3 pt-2">
            <Link
              to="/nova/c/new"
              className="px-4 py-2 rounded-xl text-sm text-text-secondary hover:text-text-primary transition-colors"
            >
              Cancel
            </Link>
            <button
              type="submit"
              disabled={isUpdating}
              className="px-6 py-2 rounded-xl bg-violet-500 text-white text-sm font-medium hover:bg-violet-400 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isUpdating ? "Saving..." : "Save Changes"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
