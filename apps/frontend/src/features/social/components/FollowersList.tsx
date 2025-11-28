/**
 * FollowersList component
 * Displays a paginated list of followers with infinite scroll support
 */

import { useFollowers } from "../hooks/useFollowers";
import { FollowButton } from "./FollowButton";
import { Card } from "../../../components/ui/Card";
import type { FollowerDto } from "../../../types";

export interface FollowersListProps {
  profileId: string;
}

interface FollowerItemProps {
  follower: FollowerDto;
  onFollowChange: () => void;
}

function FollowerItem({ follower, onFollowChange }: FollowerItemProps): JSX.Element {
  const getAvatarDisplay = (): JSX.Element => {
    if (follower.avatarUrl) {
      return (
        <img
          src={follower.avatarUrl}
          alt={follower.name}
          className="h-12 w-12 flex-shrink-0 rounded-full object-cover border border-border"
        />
      );
    }

    // Fallback to initial
    const initial = follower.name.charAt(0).toUpperCase();
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
            <p className="text-sm font-medium text-text-primary truncate">{follower.name}</p>
            <p className="text-xs text-text-tertiary truncate">@{follower.handle}</p>
          </div>
          <FollowButton profileId={follower.profileId} size="sm" onFollowChange={onFollowChange} />
        </div>
        {follower.bio && (
          <p className="mt-1 text-xs text-text-secondary line-clamp-2">{follower.bio}</p>
        )}
      </div>
    </div>
  );
}

export function FollowersList({ profileId }: FollowersListProps): JSX.Element {
  const { followers, totalCount, isLoading, error, hasMore, loadMore, refetch } = useFollowers(profileId);

  if (error) {
    return (
      <Card>
        <div className="py-8 text-center">
          <p className="text-sm text-red-500">{error}</p>
        </div>
      </Card>
    );
  }

  if (isLoading && followers.length === 0) {
    return (
      <Card>
        <div className="py-8 text-center">
          <p className="text-sm text-text-tertiary">Loading followers...</p>
        </div>
      </Card>
    );
  }

  if (followers.length === 0) {
    return (
      <Card>
        <div className="py-8 text-center">
          <p className="text-sm text-text-secondary">No followers yet</p>
        </div>
      </Card>
    );
  }

  return (
    <Card>
      <div className="mb-3">
        <h2 className="text-base font-semibold tracking-tight text-text-primary">
          Followers
        </h2>
        <p className="text-xs text-text-tertiary">
          {totalCount} {totalCount === 1 ? "follower" : "followers"}
        </p>
      </div>

      <div>
        {followers.map((follower) => (
          <FollowerItem
            key={follower.profileId}
            follower={follower}
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
