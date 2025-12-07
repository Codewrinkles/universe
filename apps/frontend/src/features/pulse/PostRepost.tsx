import type { RepulsedPulse } from "../../types";
import { formatTimeAgo } from "../../utils/timeUtils";
import { config } from "../../config";
import { parseMentions } from "./parseMentions";
import { Link } from "react-router";

interface PostRepostProps {
  repost: RepulsedPulse;
}

export function PostRepost({ repost }: PostRepostProps): JSX.Element {
  // Handle avatar URL - if it's a relative path, prepend base URL
  const avatarUrl = repost.author.avatarUrl
    ? (repost.author.avatarUrl.startsWith('http') ? repost.author.avatarUrl : `${config.api.baseUrl}${repost.author.avatarUrl}`)
    : null;

  // Format createdAt to relative time
  const timeAgo = formatTimeAgo(repost.createdAt);

  // If pulse is deleted, show placeholder
  if (repost.isDeleted) {
    return (
      <div className="mt-3 rounded-2xl border border-border p-3 bg-surface-card2/30">
        <p className="text-sm text-text-tertiary italic">This pulse has been deleted</p>
      </div>
    );
  }

  return (
    <div className="mt-3 rounded-2xl border border-border p-3 hover:bg-surface-card1/50 transition-colors cursor-pointer">
      {/* Reposted author info */}
      <div className="flex items-center gap-2">
        {avatarUrl ? (
          <img
            src={avatarUrl}
            alt={repost.author.name}
            className="h-5 w-5 rounded-full object-cover"
          />
        ) : (
          <div className="flex h-5 w-5 items-center justify-center rounded-full bg-surface-card2 border border-border text-[10px] font-semibold text-text-primary">
            {repost.author.name.charAt(0).toUpperCase()}
          </div>
        )}
        <div className="flex items-center gap-1 text-sm min-w-0">
          <span className="font-medium text-text-primary truncate">
            {repost.author.name}
          </span>
          <span className="text-text-tertiary truncate">@{repost.author.handle}</span>
          <span className="text-text-tertiary">·</span>
          <span className="text-text-tertiary">{timeAgo}</span>
        </div>
      </div>

      {/* Reposted content */}
      <p className="mt-2 text-sm text-text-primary line-clamp-3">
        {parseMentions(repost.content, []).map((part, index) => {
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

      {/* Reposted image (single only in backend for now) */}
      {repost.image && (
        <div className="mt-2 overflow-hidden rounded-xl">
          <img
            src={repost.image.url}
            alt={repost.image.altText ?? "Image"}
            className="h-32 w-full object-cover"
          />
        </div>
      )}

      {/* Reposted link preview (compact version) */}
      {repost.linkPreview && (
        <div className="mt-2 flex gap-2 rounded-xl border border-border overflow-hidden">
          {repost.linkPreview.imageUrl && (
            <img
              src={repost.linkPreview.imageUrl}
              alt={repost.linkPreview.title}
              className="w-20 h-20 object-cover flex-shrink-0"
            />
          )}
          <div className="py-2 pr-2 flex flex-col justify-center min-w-0">
            <p className="text-[10px] text-text-tertiary flex items-center gap-1">
              <span>🔗</span>
              <span>{repost.linkPreview.domain}</span>
            </p>
            <p className="text-xs font-medium text-text-primary line-clamp-2">
              {repost.linkPreview.title}
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
