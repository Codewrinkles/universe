import { useState } from "react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";
import { useAuth } from "../../../hooks/useAuth";

interface CompleteProfileProps {
  onComplete: () => void;
}

export function CompleteProfile({ onComplete }: CompleteProfileProps): JSX.Element {
  const { user, updateProfile } = useAuth();
  const [handle, setHandle] = useState(user?.handle || "");
  const [bio, setBio] = useState(user?.bio || "");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      await updateProfile({
        name: user?.name || "",
        handle: handle.trim() || null,
        bio: bio.trim() || null,
        location: user?.location || null,
        websiteUrl: user?.websiteUrl || null,
      });

      onComplete();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update profile");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Card>
      <h2 className="text-xl font-bold text-text-primary mb-2">
        Complete Your Profile
      </h2>
      <p className="text-sm text-text-secondary mb-6">
        Let people know who you are
      </p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-text-primary mb-2">
            Handle
          </label>
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary">
              @
            </span>
            <input
              type="text"
              value={handle}
              onChange={(e) => setHandle(e.target.value)}
              className="w-full pl-8 pr-3 py-2 bg-surface-card2 border border-border rounded-lg text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-brand"
              placeholder="yourhandle"
              required
              minLength={3}
              maxLength={30}
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-text-primary mb-2">
            Bio
          </label>
          <textarea
            value={bio}
            onChange={(e) => setBio(e.target.value)}
            className="w-full px-3 py-2 bg-surface-card2 border border-border rounded-lg text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-brand resize-none"
            placeholder="Tell us about yourself..."
            rows={3}
            maxLength={160}
          />
          <p className="text-xs text-text-tertiary mt-1">
            {bio.length}/160 characters
          </p>
        </div>

        {error && (
          <div className="text-sm text-red-400">
            {error}
          </div>
        )}

        <div className="flex justify-end">
          <Button
            type="submit"
            variant="primary"
            disabled={isSubmitting || !handle.trim()}
          >
            {isSubmitting ? "Saving..." : "Continue"}
          </Button>
        </div>
      </form>
    </Card>
  );
}
