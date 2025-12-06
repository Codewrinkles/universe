/**
 * FollowButton component
 * Displays a follow/unfollow button with loading and follow status
 */

import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useFollow } from "../hooks/useFollow";
import { useIsFollowing } from "../hooks/useIsFollowing";
import { useAuth } from "../../../hooks/useAuth";

export interface FollowButtonProps {
  profileId: string;
  initialIsFollowing?: boolean;
  onFollowChange?: (isFollowing: boolean) => void;
  size?: "sm" | "md";
  className?: string;
}

export function FollowButton({
  profileId,
  initialIsFollowing: initialIsFollowingProp,
  onFollowChange,
  size = "md",
  className = "",
}: FollowButtonProps): JSX.Element {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { follow, unfollow, isLoading: isActionLoading } = useFollow();

  // Only fetch follow status if not provided as prop
  const shouldFetchFollowStatus = initialIsFollowingProp === undefined;
  const { isFollowing: fetchedIsFollowing, isLoading: isCheckLoading, refetch } = useIsFollowing(
    shouldFetchFollowStatus ? profileId : ""
  );

  // Use prop if provided, otherwise use fetched value
  const initialIsFollowing = initialIsFollowingProp ?? fetchedIsFollowing;
  const [isFollowing, setIsFollowing] = useState(initialIsFollowing);
  const [isHovering, setIsHovering] = useState(false);

  // Update local state when remote state or prop changes
  useEffect(() => {
    setIsFollowing(initialIsFollowing);
  }, [initialIsFollowing]);

  const handleToggleFollow = async (): Promise<void> => {
    try {
      // Optimistic update
      const newFollowState = !isFollowing;
      setIsFollowing(newFollowState);

      if (isFollowing) {
        await unfollow(profileId);
      } else {
        await follow(profileId);
      }

      // Refetch to ensure consistency
      refetch();

      // Notify parent component
      onFollowChange?.(newFollowState);
    } catch (error) {
      // Revert optimistic update on error
      setIsFollowing(!isFollowing);
      console.error("Failed to toggle follow:", error);
    }
  };

  const handleLoginClick = (): void => {
    navigate("/login");
  };

  // Don't show button if viewing own profile
  if (user?.profileId === profileId) {
    return <></>;
  }

  const sizeClasses = {
    sm: "px-3 py-1 text-xs",
    md: "px-3 py-1.5 text-xs",
  };

  // Show login prompt for unauthenticated users
  if (!user) {
    return (
      <button
        type="button"
        onClick={handleLoginClick}
        className={`flex-shrink-0 rounded-full bg-brand-soft text-black hover:bg-brand transition-colors font-semibold ${sizeClasses[size]} ${className}`}
      >
        Log in to follow
      </button>
    );
  }

  const isLoading = (shouldFetchFollowStatus && isCheckLoading) || isActionLoading;

  if (isFollowing) {
    // Following - show unfollow button (secondary style)
    return (
      <button
        type="button"
        onClick={handleToggleFollow}
        onMouseEnter={() => setIsHovering(true)}
        onMouseLeave={() => setIsHovering(false)}
        disabled={isLoading}
        className={`flex-shrink-0 rounded-full border border-border bg-surface-card1 text-text-secondary hover:border-red-500/60 hover:bg-red-500/10 hover:text-red-500 transition-colors font-semibold disabled:opacity-50 disabled:cursor-not-allowed ${sizeClasses[size]} ${className}`}
      >
        {isLoading ? "..." : isHovering ? "Unfollow" : "Following"}
      </button>
    );
  }

  // Not following - show follow button (primary style with brand colors)
  return (
    <button
      type="button"
      onClick={handleToggleFollow}
      disabled={isLoading}
      className={`flex-shrink-0 rounded-full bg-brand-soft text-black hover:bg-brand transition-colors font-semibold disabled:opacity-50 disabled:cursor-not-allowed ${sizeClasses[size]} ${className}`}
    >
      {isLoading ? "..." : "Follow"}
    </button>
  );
}
