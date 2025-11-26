import type { RepostedPost } from "../../types";

interface PostRepostProps {
  repost: RepostedPost;
}

export function PostRepost({ repost }: PostRepostProps): JSX.Element {
  return (
    <div className="mt-3 rounded-2xl border border-border p-3 hover:bg-surface-card1/50 transition-colors cursor-pointer">
      {/* Reposted author info */}
      <div className="flex items-center gap-2">
        {repost.author.avatarUrl ? (
          <img
            src={repost.author.avatarUrl}
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
          <span className="text-text-tertiary">{repost.timeAgo}</span>
        </div>
      </div>

      {/* Reposted content */}
      <p className="mt-2 text-sm text-text-primary line-clamp-3">
        {repost.content}
      </p>

      {/* Reposted images (if any, show small preview) */}
      {repost.images && repost.images.length > 0 && (
        <div className="mt-2 flex gap-1 overflow-hidden rounded-xl">
          {repost.images.slice(0, 4).map((image, index) => (
            <img
              key={index}
              src={image.url}
              alt={image.alt ?? `Image ${index + 1}`}
              className="h-16 w-16 object-cover rounded-lg"
            />
          ))}
          {repost.images.length > 4 && (
            <div className="h-16 w-16 rounded-lg bg-surface-card2 flex items-center justify-center text-xs text-text-tertiary">
              +{repost.images.length - 4}
            </div>
          )}
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
