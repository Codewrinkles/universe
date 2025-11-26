import type { Post } from "../../types";
import { PostImages } from "./PostImages";
import { PostVideo } from "./PostVideo";
import { PostLinkPreview } from "./PostLinkPreview";
import { PostRepost } from "./PostRepost";

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
}

function ActionButton({ icon, count, hoverColor = "hover:text-brand-soft hover:bg-brand-soft/10", label }: ActionButtonProps): JSX.Element {
  return (
    <button
      type="button"
      title={label}
      className={`group flex items-center gap-1.5 rounded-full p-2 text-text-tertiary transition-colors ${hoverColor}`}
    >
      <span className="text-base">{icon}</span>
      {count !== undefined && count > 0 && (
        <span className="text-xs">{formatCount(count)}</span>
      )}
    </button>
  );
}

export function PostCard({ post }: PostCardProps): JSX.Element {
  const { author } = post;

  return (
    <article className="px-4 py-3 hover:bg-surface-card1/50 transition-colors cursor-pointer">
      <div className="flex gap-3">
        {/* Avatar */}
        <div className="flex-shrink-0">
          {author.avatarUrl ? (
            <img
              src={author.avatarUrl}
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
            <span className="text-text-tertiary hover:underline">{post.timeAgo}</span>
          </div>

          {/* Post text content */}
          {post.content && (
            <p className="mt-1 text-[15px] leading-normal text-text-primary whitespace-pre-wrap">
              {post.content}
            </p>
          )}

          {/* Photo attachment */}
          {post.images && post.images.length > 0 && (
            <PostImages images={post.images} />
          )}

          {/* Video attachment */}
          {post.video && (
            <PostVideo video={post.video} />
          )}

          {/* Link preview */}
          {post.linkPreview && (
            <PostLinkPreview link={post.linkPreview} />
          )}

          {/* Reposted content (quote tweet) */}
          {post.repostedPost && (
            <PostRepost repost={post.repostedPost} />
          )}

          {/* Thread indicator */}
          {post.isThread && (
            <div className="mt-3 text-sm text-brand-soft hover:underline cursor-pointer">
              Show this thread{post.threadLength ? ` (${post.threadLength} posts)` : ""}
            </div>
          )}

          {/* Actions */}
          <div className="mt-2 flex items-center justify-between max-w-md -ml-2">
            <ActionButton
              icon="💬"
              count={post.replyCount}
              hoverColor="hover:text-sky-400 hover:bg-sky-400/10"
              label="Reply"
            />
            <ActionButton
              icon="🔄"
              count={post.repostCount}
              hoverColor="hover:text-green-400 hover:bg-green-400/10"
              label="Repost"
            />
            <ActionButton
              icon="❤️"
              count={post.likeCount}
              hoverColor="hover:text-pink-400 hover:bg-pink-400/10"
              label="Like"
            />
            <ActionButton
              icon="📊"
              count={post.viewCount}
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
