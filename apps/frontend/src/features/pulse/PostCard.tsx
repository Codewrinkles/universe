import { useState, useRef, useEffect } from "react";
import { useNavigate, Link } from "react-router";
import type { Post } from "../../types";
import { PostLinkPreview } from "./PostLinkPreview";
import { PostRepost } from "./PostRepost";
import { RepulseModal } from "./RepulseModal";
import { DeleteConfirmDialog } from "./DeleteConfirmDialog";
import { ImageGalleryOverlay } from "../../components/ui";
import { formatTimeAgo } from "../../utils/timeUtils";
import { config } from "../../config";
import { usePulseLike } from "./hooks/usePulseLike";
import { FollowButton } from "../social/components/FollowButton";
import { parseMentions } from "./parseMentions";
import { pulseApi } from "../../services/pulseApi";
import { useAuth } from "../../hooks/useAuth";

export interface PostCardProps {
  post: Post;
  onReplyClick?: (pulseId: string) => void;
  onFollowChange?: () => void;
  onDelete?: (pulseId: string) => void;
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

export function PostCard({ post, onReplyClick, onFollowChange, onDelete }: PostCardProps): JSX.Element {
  const { author } = post;
  const { user } = useAuth();
  const navigate = useNavigate();
  const menuRef = useRef<HTMLDivElement>(null);
  const [showRepulseModal, setShowRepulseModal] = useState(false);
  const [showMenu, setShowMenu] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isBookmarked, setIsBookmarked] = useState(post.isBookmarkedByCurrentUser);
  const [showImageOverlay, setShowImageOverlay] = useState(false);

  // Check if current user is the author
  const isAuthor = user?.profileId === author.id;

  // Ensure we have a valid handle for profile links
  const profileHandle = author.handle || 'unknown';

  // Close menu when clicking outside
  useEffect(() => {
    if (!showMenu) return;

    const handleClickOutside = (event: MouseEvent): void => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setShowMenu(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [showMenu]);

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

  // Extract like status
  const isLikedByCurrentUser = 'isLikedByCurrentUser' in post ? post.isLikedByCurrentUser : false;

  const handleCardClick = (): void => {
    // If this is a reply, navigate to the thread root and highlight this reply
    if (post.type === "reply") {
      // Use threadRootId if available, otherwise fall back to parentPulseId
      const threadRoot = post.threadRootId || post.parentPulseId;
      if (threadRoot) {
        navigate(`/pulse/${threadRoot}?highlight=${post.id}`);
        return;
      }
    }

    // Otherwise, navigate to this pulse's own thread
    navigate(`/pulse/${post.id}`);
  };

  const handleReplyButtonClick = (): void => {
    // If onReplyClick prop is provided (we're in ThreadView), use it
    if (onReplyClick) {
      onReplyClick(post.id);
      return;
    }

    // Otherwise, navigate to the thread (we're in feed view)
    // Use threadRootId if available, otherwise fall back to parentPulseId
    const targetPulseId = post.type === "reply"
      ? (post.threadRootId || post.parentPulseId || post.id)
      : post.id;
    navigate(`/pulse/${targetPulseId}`);
  };

  const handleDeleteClick = (): void => {
    setShowMenu(false);
    setShowDeleteDialog(true);
  };

  const handleDeleteConfirm = async (): Promise<void> => {
    setIsDeleting(true);
    try {
      await pulseApi.deletePulse(post.id);
      setShowDeleteDialog(false);
      // Notify parent to remove pulse from state
      onDelete?.(post.id);
    } catch (error) {
      console.error("Failed to delete pulse:", error);
      alert("Failed to delete pulse. Please try again.");
    } finally {
      setIsDeleting(false);
    }
  };

  const handleBookmarkClick = async (): Promise<void> => {
    try {
      if (isBookmarked) {
        await pulseApi.unbookmarkPulse(post.id);
        setIsBookmarked(false);
      } else {
        await pulseApi.bookmarkPulse(post.id);
        setIsBookmarked(true);
      }
    } catch (error) {
      console.error("Failed to toggle bookmark:", error);
    }
  };

  return (
    <>
    <article
      className="px-4 py-3 hover:bg-surface-card1/50 transition-colors cursor-pointer"
      onClick={handleCardClick}
    >
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
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-1 text-sm min-w-0">
              <Link
                to={`/pulse/u/${profileHandle}`}
                onClick={(e) => e.stopPropagation()}
                className="font-semibold text-text-primary hover:underline truncate"
              >
                {author.name}
              </Link>
              <Link
                to={`/pulse/u/${profileHandle}`}
                onClick={(e) => e.stopPropagation()}
                className="text-text-tertiary hover:underline truncate"
              >
                @{profileHandle}
              </Link>
              <span className="text-text-tertiary">·</span>
              <span className="text-text-tertiary hover:underline">{timeAgo}</span>
            </div>
            <div
              onClick={(e) => e.stopPropagation()}
              className="flex items-center gap-2 flex-shrink-0"
            >
              {/* Follow button - only show if authenticated and not viewing own profile */}
              {user && !isAuthor && (
                <FollowButton
                  profileId={author.id}
                  initialIsFollowing={post.isFollowingAuthor}
                  size="sm"
                  onFollowChange={onFollowChange}
                />
              )}
              {/* Three-dot menu for author's own posts */}
              {user && isAuthor && (
                <div className="relative" ref={menuRef}>
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      setShowMenu(!showMenu);
                    }}
                    className="flex h-8 w-8 items-center justify-center rounded-full text-text-tertiary hover:bg-brand-soft/10 hover:text-brand-soft transition-colors"
                    aria-label="More options"
                  >
                    <span className="text-base">⋯</span>
                  </button>
                  {/* Dropdown menu */}
                  {showMenu && (
                    <div className="absolute right-0 top-full mt-1 w-48 rounded-xl border border-border bg-surface-card1 shadow-lg z-10">
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDeleteClick();
                        }}
                        className="flex w-full items-center gap-3 px-4 py-3 text-sm text-red-500 hover:bg-red-500/10 transition-colors rounded-xl"
                      >
                        <span>🗑️</span>
                        <span>Delete</span>
                      </button>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>

          {/* Replying to indicator for nested replies */}
          {post.replyingTo && (
            <div className="mt-1 text-sm text-text-tertiary">
              Replying to{" "}
              <Link
                to={`/pulse/u/${post.replyingTo.authorHandle}`}
                onClick={(e) => e.stopPropagation()}
                className="text-brand-soft hover:underline"
              >
                @{post.replyingTo.authorHandle}
              </Link>
            </div>
          )}

          {/* Post text content */}
          {post.content && (
            <p className="mt-1 text-[15px] leading-normal text-text-primary whitespace-pre-wrap">
              {parseMentions(post.content, post.mentions || []).map((part, index) => {
                if (part.type === "mention" && part.handle) {
                  return (
                    <Link
                      key={index}
                      to={`/pulse/u/${part.handle}`}
                      className="text-brand-link hover:underline"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {part.content}
                    </Link>
                  );
                }
                if (part.type === "hashtag" && part.tag) {
                  return (
                    <Link
                      key={index}
                      to={`/social/hashtag/${part.tag.toLowerCase()}`}
                      className="text-brand-link hover:underline"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {part.content}
                    </Link>
                  );
                }
                if (part.type === "url" && part.url) {
                  return (
                    <a
                      key={index}
                      href={part.url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-brand-link hover:underline"
                      onClick={(e) => e.stopPropagation()}
                    >
                      {part.content}
                    </a>
                  );
                }
                return <span key={index}>{part.content}</span>;
              })}
            </p>
          )}

          {/* Photo attachment */}
          {post.imageUrl && (
            <div
              className="mt-3 rounded-2xl overflow-hidden border border-border cursor-pointer hover:opacity-90 transition-opacity"
              onClick={(e) => {
                e.stopPropagation(); // Prevent navigation to thread
                setShowImageOverlay(true);
              }}
            >
              <img
                src={post.imageUrl.startsWith('http') ? post.imageUrl : `${config.api.baseUrl}${post.imageUrl}`}
                alt="Pulse attachment"
                className="w-full object-contain max-h-[500px]"
              />
            </div>
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

          {/* Actions - only show if authenticated */}
          {user && (
            <div className="mt-2 flex items-center justify-between max-w-md -ml-2">
              <ActionButton
                icon="💬"
                count={replyCount}
                hoverColor="hover:text-sky-400 hover:bg-sky-400/10"
                label="Reply"
                onClick={handleReplyButtonClick}
              />
              <ActionButton
                icon="🔄"
                count={repostCount}
                hoverColor="hover:text-green-400 hover:bg-green-400/10"
                label="Re-Pulse"
                onClick={() => setShowRepulseModal(true)}
              />
              <LikeButton
                pulseId={post.id}
                initialIsLiked={isLikedByCurrentUser}
                initialLikeCount={likeCount}
              />
              <ActionButton
                icon={isBookmarked ? "🔖" : "📑"}
                hoverColor="hover:text-brand-soft hover:bg-brand-soft/10"
                label={isBookmarked ? "Remove bookmark" : "Bookmark"}
                onClick={handleBookmarkClick}
              />
            </div>
          )}
        </div>
      </div>
    </article>
    {/* Repulse Modal - rendered outside article to prevent event bubbling */}
    {showRepulseModal && (
      <RepulseModal
        post={post}
        onClose={() => setShowRepulseModal(false)}
        onSuccess={onFollowChange}
      />
    )}
    {/* Delete Confirmation Dialog */}
    {showDeleteDialog && (
      <DeleteConfirmDialog
        onConfirm={handleDeleteConfirm}
        onCancel={() => setShowDeleteDialog(false)}
        isDeleting={isDeleting}
      />
    )}

    {/* Image Gallery Overlay */}
    {showImageOverlay && post.imageUrl && (
      <ImageGalleryOverlay
        imageUrl={post.imageUrl.startsWith('http') ? post.imageUrl : `${config.api.baseUrl}${post.imageUrl}`}
        altText="Pulse attachment"
        onClose={() => setShowImageOverlay(false)}
      />
    )}
    </>
  );
}
