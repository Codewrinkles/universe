import { useState, useEffect } from "react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";
import { FollowButton } from "../../social/components/FollowButton";
import { config } from "../../../config";
import type { ProfileSuggestion } from "../../../types";

interface SuggestedFollowsProps {
  onComplete: () => void;
}

export function SuggestedFollows({ onComplete }: SuggestedFollowsProps): JSX.Element {
  const [profiles, setProfiles] = useState<ProfileSuggestion[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function fetchPopular(): Promise<void> {
      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        const response = await fetch(`${config.api.baseUrl}/api/social/popular?limit=10`, {
          headers: {
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
        });

        if (!response.ok) {
          throw new Error("Failed to fetch popular profiles");
        }

        const data = await response.json() as { profiles: ProfileSuggestion[] };
        setProfiles(data.profiles);
      } catch (error) {
        console.error("Failed to fetch popular profiles:", error);
      } finally {
        setIsLoading(false);
      }
    }

    void fetchPopular();
  }, []);

  const getAvatarDisplay = (profile: ProfileSuggestion): JSX.Element => {
    if (profile.avatarUrl) {
      return (
        <img
          src={`${config.api.baseUrl}${profile.avatarUrl}`}
          alt={profile.name}
          className="h-12 w-12 rounded-full object-cover"
        />
      );
    }

    const initial = profile.name.charAt(0).toUpperCase();
    return (
      <div className="h-12 w-12 rounded-full bg-surface-card1 flex items-center justify-center text-lg font-semibold text-text-primary">
        {initial}
      </div>
    );
  };

  return (
    <Card>
      <h2 className="text-xl font-bold text-text-primary mb-2">
        Follow People You're Interested In
      </h2>
      <p className="text-sm text-text-secondary mb-6">
        Start building your network (you can skip this step)
      </p>

      {isLoading ? (
        <div className="py-8 text-center">
          <p className="text-sm text-text-tertiary">Loading suggestions...</p>
        </div>
      ) : profiles.length === 0 ? (
        <div className="py-8 text-center">
          <p className="text-sm text-text-tertiary">No suggestions available</p>
        </div>
      ) : (
        <div className="space-y-4 mb-6 max-h-96 overflow-y-auto custom-scrollbar">
          {profiles.map((profile) => (
            <div key={profile.profileId} className="flex items-start gap-3 p-3 bg-surface-card2 rounded-lg">
              {getAvatarDisplay(profile)}
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-text-primary truncate">
                  {profile.name}
                </p>
                <p className="text-xs text-text-tertiary truncate">
                  @{profile.handle}
                </p>
                {profile.bio && (
                  <p className="text-xs text-text-secondary mt-1 line-clamp-2">
                    {profile.bio}
                  </p>
                )}
              </div>
              <FollowButton profileId={profile.profileId} size="sm" />
            </div>
          ))}
        </div>
      )}

      <div className="flex gap-3">
        <Button variant="secondary" onClick={onComplete}>
          Skip for Now
        </Button>
        <Button variant="primary" onClick={onComplete}>
          Continue to Pulse
        </Button>
      </div>
    </Card>
  );
}
