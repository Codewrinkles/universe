import { Card } from "../../components/ui/Card";
import { useSuggestedProfiles } from "../social/hooks/useSuggestedProfiles";
import { FollowButton } from "../social/components/FollowButton";
import { useAuth } from "../../hooks/useAuth";
import type { ProfileSuggestion } from "../../types";

interface UserSuggestionProps {
  user: ProfileSuggestion;
  onFollowChange: () => void;
}

function UserSuggestion({ user, onFollowChange }: UserSuggestionProps): JSX.Element {
  const getAvatarDisplay = (): JSX.Element => {
    if (user.avatarUrl) {
      return (
        <img
          src={user.avatarUrl}
          alt={user.name}
          className="h-10 w-10 flex-shrink-0 rounded-full object-cover border border-border"
        />
      );
    }

    // Fallback to initial
    const initial = user.name.charAt(0).toUpperCase();
    return (
      <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-surface-card2 border border-border text-sm font-semibold text-text-primary">
        {initial}
      </div>
    );
  };

  return (
    <div className="flex items-start gap-3 py-3">
      {getAvatarDisplay()}
      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="text-sm font-medium text-text-primary truncate">{user.name}</p>
            <p className="text-xs text-text-tertiary truncate">@{user.handle}</p>
          </div>
          <FollowButton profileId={user.profileId} size="sm" onFollowChange={onFollowChange} />
        </div>
        {user.bio && (
          <p className="mt-1 text-xs text-text-secondary line-clamp-2">{user.bio}</p>
        )}
        {user.mutualFollowCount > 0 && (
          <p className="mt-1 text-xs text-text-tertiary">
            Followed by {user.mutualFollowCount} {user.mutualFollowCount === 1 ? "person" : "people"} you follow
          </p>
        )}
      </div>
    </div>
  );
}

export function WhoToFollow(): JSX.Element {
  const { user } = useAuth();
  const { suggestions, isLoading, refetch } = useSuggestedProfiles(3);

  // Don't show if not authenticated
  if (!user) {
    return <></>;
  }

  // Don't show if loading and no suggestions yet
  if (isLoading && suggestions.length === 0) {
    return (
      <Card>
        <h2 className="text-base font-semibold tracking-tight text-text-primary">
          Who to follow
        </h2>
        <div className="py-4 text-center">
          <p className="text-xs text-text-tertiary">Loading suggestions...</p>
        </div>
      </Card>
    );
  }

  // Don't show if no suggestions
  if (suggestions.length === 0) {
    return <></>;
  }

  return (
    <Card>
      <h2 className="text-base font-semibold tracking-tight text-text-primary">
        Who to follow
      </h2>
      <div className="divide-y divide-border">
        {suggestions.map((suggestion) => (
          <UserSuggestion
            key={suggestion.profileId}
            user={suggestion}
            onFollowChange={refetch}
          />
        ))}
      </div>
    </Card>
  );
}
