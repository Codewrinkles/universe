/**
 * FollowingList component
 * Displays a paginated list of profiles the user is following with infinite scroll support
 */

import { useFollowing } from "../hooks/useFollowing";
import { FollowButton } from "./FollowButton";
import { Card } from "../../../components/ui/Card";
import type { FollowingDto } from "../../../types";

export interface FollowingListProps {
  profileId: string;
}

interface FollowingItemProps {
  following: FollowingDto;
  onFollowChange: () => void;
}

function FollowingItem({ following, onFollowChange }: FollowingItemProps): JSX.Element {
  const getAvatarDisplay = (): JSX.Element => {
    if (following.avatarUrl) {
      return (
        <img
          src={following.avatarUrl}
          alt={following.name}
          className="h-12 w-12 flex-shrink-0 rounded-full object-cover border border-border"
        />
      );
    }

    // Fallback to initial
    const initial = following.name.charAt(0).toUpperCase();
    return (
      <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-full bg-surface-card2 border border-border text-sm font-semibold text-text-primary">
        {initial}
      </div>
    );
  };

  return (
    <div className="flex items-start gap-3 py-3 border-b border-border last:border-b-0">
      {getAvatarDisplay()}
      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="text-sm font-medium text-text-primary truncate">{following.name}</p>
            <p className="text-xs text-text-tertiary truncate">@{following.handle}</p>
          </div>
          <FollowButton profileId={following.profileId} size="sm" onFollowChange={onFollowChange} />
        </div>
        {following.bio && (
          <p className="mt-1 text-xs text-text-secondary line-clamp-2">{following.bio}</p>
        )}
      </div>
    </div>
  );
}

export function FollowingList({ profileId }: FollowingListProps): JSX.Element {
  const { following, totalCount, isLoading, error, hasMore, loadMore, refetch } = useFollowing(profileId);

  if (error) {
    return (
      <Card>
        <div className="py-8 text-center">
          <p className="text-sm text-red-500">{error}</p>
        </div>
      </Card>
    );
  }

  if (isLoading && following.length === 0) {
    return (
      <Card>
        <div className="py-8 text-center">
          <p className="text-sm text-text-tertiary">Loading following...</p>
        </div>
      </Card>
    );
  }

  if (following.length === 0) {
    return (
      <Card>
        <div className="py-8 text-center">
          <p className="text-sm text-text-secondary">Not following anyone yet</p>
        </div>
      </Card>
    );
  }

  return (
    <Card>
      <div className="mb-3">
        <h2 className="text-base font-semibold tracking-tight text-text-primary">
          Following
        </h2>
        <p className="text-xs text-text-tertiary">
          {totalCount} {totalCount === 1 ? "profile" : "profiles"}
        </p>
      </div>

      <div>
        {following.map((followingProfile) => (
          <FollowingItem
            key={followingProfile.profileId}
            following={followingProfile}
            onFollowChange={refetch}
          />
        ))}
      </div>

      {hasMore && (
        <div className="mt-4 text-center">
          <button
            type="button"
            onClick={loadMore}
            disabled={isLoading}
            className="text-xs text-brand-soft hover:text-brand transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? "Loading..." : "Load more"}
          </button>
        </div>
      )}
    </Card>
  );
}
