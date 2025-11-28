import { useState } from "react";
import type { Post } from "../../types";
import { PostImages } from "./PostImages";
import { PostLinkPreview } from "./PostLinkPreview";
import { PostRepost } from "./PostRepost";
import { formatTimeAgo } from "../../utils/timeUtils";
import { config } from "../../config";
import { usePulseLike } from "./hooks/usePulseLike";

export interface PostCardProps {
  post: Post;
}

function formatCount(count: number): string {
  if (count >= 1000000) {
    return `${(count / 1000000).toFixed(1)}M`;
  }
  if (count >= 1000) {
    return `${(count / 1000).toFixed(1)}K`;
  }
  return count.toString();
}

interface ActionButtonProps {
  icon: string;
  count?: number;
  hoverColor?: string;
  label: string;
  onClick?: () => void;
}

function ActionButton({ icon, count, hoverColor = "hover:text-brand-soft hover:bg-brand-soft/10", label, onClick }: ActionButtonProps): JSX.Element {
  return (
    <button
      type="button"
      title={label}
      onClick={(e) => {
        e.stopPropagation();
        onClick?.();
      }}
      className={`group flex items-center gap-1.5 rounded-full p-2 text-text-tertiary transition-colors ${hoverColor}`}
    >
      <span className="text-base">{icon}</span>
      {count !== undefined && count > 0 && (
        <span className="text-xs">{formatCount(count)}</span>
      )}
    </button>
  );
}

interface LikeButtonProps {
  pulseId: string;
  initialIsLiked: boolean;
  initialLikeCount: number;
}

function LikeButton({ pulseId, initialIsLiked, initialLikeCount }: LikeButtonProps): JSX.Element {
  const { toggleLike, isLoading } = usePulseLike();
  const [isLiked, setIsLiked] = useState(initialIsLiked);
  const [likeCount, setLikeCount] = useState(initialLikeCount);

  const handleLikeClick = async (): Promise<void> => {
    if (isLoading) return;

    // Optimistic update
    const newIsLiked = !isLiked;
    const newLikeCount = newIsLiked ? likeCount + 1 : Math.max(0, likeCount - 1);

    setIsLiked(newIsLiked);
    setLikeCount(newLikeCount);

    try {
      await toggleLike(pulseId, isLiked);
    } catch (error) {
      // Rollback on error
      setIsLiked(isLiked);
      setLikeCount(likeCount);
      console.error("Failed to toggle like:", error);
    }
  };

  return (
    <button
      type="button"
      title={isLiked ? "Unlike" : "Like"}
      onClick={(e) => {
        e.stopPropagation();
        void handleLikeClick();
      }}
      disabled={isLoading}
      className={`group flex items-center gap-1.5 rounded-full p-2 transition-colors ${
        isLiked
          ? "text-pink-400"
          : "text-text-tertiary hover:text-pink-400 hover:bg-pink-400/10"
      }`}
    >
      <span className="text-base">{isLiked ? "💖" : "❤️"}</span>
      {likeCount > 0 && (
        <span className="text-xs">{formatCount(likeCount)}</span>
      )}
    </button>
  );
}

export function PostCard({ post }: PostCardProps): JSX.Element {
  const { author } = post;

  // Handle avatar URL - if it's a relative path, prepend base URL
  const avatarUrl = author.avatarUrl
    ? (author.avatarUrl.startsWith('http') ? author.avatarUrl : `${config.api.baseUrl}${author.avatarUrl}`)
    : null;

  // Format createdAt to relative time
  const timeAgo = 'createdAt' in post ? formatTimeAgo(post.createdAt) : (post as any).timeAgo || '';

  // Extract engagement counts (handle both new and legacy formats)
  const replyCount = 'engagement' in post ? post.engagement.replyCount : (post as any).replyCount;
  const repostCount = 'engagement' in post ? post.engagement.repulseCount : (post as any).repostCount;
  const likeCount = 'engagement' in post ? post.engagement.likeCount : (post as any).likeCount;
  const viewCount = 'engagement' in post ? post.engagement.viewCount : (post as any).viewCount;

  // Extract like status
  const isLikedByCurrentUser = 'isLikedByCurrentUser' in post ? post.isLikedByCurrentUser : false;

  return (
    <article className="px-4 py-3 hover:bg-surface-card1/50 transition-colors cursor-pointer">
      <div className="flex gap-3">
        {/* Avatar */}
        <div className="flex-shrink-0">
          {avatarUrl ? (
            <img
              src={avatarUrl}
              alt={author.name}
              className="h-10 w-10 rounded-full object-cover"
            />
          ) : (
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-surface-card2 text-sm font-semibold text-text-primary border border-border">
              {author.name.charAt(0).toUpperCase()}
            </div>
          )}
        </div>

        {/* Content */}
        <div className="flex-1 min-w-0">
          {/* Header */}
          <div className="flex items-center gap-1 text-sm">
            <span className="font-semibold text-text-primary hover:underline truncate">
              {author.name}
            </span>
            <span className="text-text-tertiary truncate">@{author.handle}</span>
            <span className="text-text-tertiary">·</span>
            <span className="text-text-tertiary hover:underline">{timeAgo}</span>
          </div>

          {/* Post text content */}
          {post.content && (
            <p className="mt-1 text-[15px] leading-normal text-text-primary whitespace-pre-wrap">
              {post.content}
            </p>
          )}

          {/* Photo attachment */}
          {'image' in post && post.image && (
            <PostImages images={[post.image]} />
          )}
          {(post as any).images && (post as any).images.length > 0 && (
            <PostImages images={(post as any).images} />
          )}

          {/* Link preview */}
          {post.linkPreview && (
            <PostLinkPreview link={post.linkPreview} />
          )}

          {/* Reposted content (quote tweet) / Repulsed pulse */}
          {'repulsedPulse' in post && post.repulsedPulse && (
            <PostRepost repost={post.repulsedPulse} />
          )}
          {(post as any).repostedPost && (
            <PostRepost repost={(post as any).repostedPost} />
          )}

          {/* Thread indicator */}
          {(post as any).isThread && (
            <div className="mt-3 text-sm text-brand-soft hover:underline cursor-pointer">
              Show this thread{(post as any).threadLength ? ` (${(post as any).threadLength} posts)` : ""}
            </div>
          )}

          {/* Actions */}
          <div className="mt-2 flex items-center justify-between max-w-md -ml-2">
            <ActionButton
              icon="💬"
              count={replyCount}
              hoverColor="hover:text-sky-400 hover:bg-sky-400/10"
              label="Reply"
            />
            <ActionButton
              icon="🔄"
              count={repostCount}
              hoverColor="hover:text-green-400 hover:bg-green-400/10"
              label="Repost"
            />
            <LikeButton
              pulseId={post.id}
              initialIsLiked={isLikedByCurrentUser}
              initialLikeCount={likeCount}
            />
            <ActionButton
              icon="📊"
              count={viewCount}
              hoverColor="hover:text-brand-soft hover:bg-brand-soft/10"
              label="Views"
            />
            <ActionButton
              icon="↗️"
              hoverColor="hover:text-brand-soft hover:bg-brand-soft/10"
              label="Share"
            />
          </div>
        </div>
      </div>
    </article>
  );
}
