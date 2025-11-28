import type { Post } from "../../types";
import { UnifiedComposer } from "./UnifiedComposer";
import { config } from "../../config";
import { formatTimeAgo } from "../../utils/timeUtils";

export interface RepulseModalProps {
  post: Post;
  onClose: () => void;
  onSuccess?: () => void;
}

export function RepulseModal({ post, onClose, onSuccess }: RepulseModalProps): JSX.Element {
  const postAvatarUrl = post.author.avatarUrl
    ? (post.author.avatarUrl.startsWith('http') ? post.author.avatarUrl : `${config.api.baseUrl}${post.author.avatarUrl}`)
    : null;

  const timeAgo = formatTimeAgo(post.createdAt);

  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>): void => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  const handleSuccess = (): void => {
    onSuccess?.();
    onClose();
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center bg-black/70 pt-20"
      onClick={handleBackdropClick}
    >
      <div
        className="w-full max-w-[600px] bg-surface-card1 rounded-2xl border border-border mx-4 max-h-[80vh] overflow-y-auto custom-scrollbar"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="sticky top-0 z-10 flex items-center justify-between border-b border-border bg-surface-card1/90 backdrop-blur px-4 py-3">
          <button
            type="button"
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-full hover:bg-surface-card2 transition-colors text-text-primary"
            title="Close"
          >
            ✕
          </button>
          <h2 className="text-base font-semibold text-text-primary">Quote Pulse</h2>
          <div className="w-8" /> {/* Spacer for symmetry */}
        </div>

        {/* Body */}
        <div className="p-4 space-y-4">
          {/* Original pulse being repulsed */}
          <div className="rounded-2xl border border-border p-3 bg-surface-card2/30">
            <div className="flex gap-2">
              {postAvatarUrl ? (
                <img
                  src={postAvatarUrl}
                  alt={post.author.name}
                  className="h-8 w-8 rounded-full object-cover flex-shrink-0"
                />
              ) : (
                <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-surface-card2 border border-border text-xs font-semibold text-text-primary">
                  {post.author.name.charAt(0).toUpperCase()}
                </div>
              )}
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-1 text-sm">
                  <span className="font-medium text-text-primary truncate">{post.author.name}</span>
                  <span className="text-text-tertiary truncate">@{post.author.handle}</span>
                  <span className="text-text-tertiary">·</span>
                  <span className="text-text-tertiary">{timeAgo}</span>
                </div>
                <p className="mt-1 text-sm text-text-primary line-clamp-5">{post.content}</p>
                {post.imageUrl && (
                  <div className="mt-2 rounded-xl overflow-hidden border border-border">
                    <img
                      src={`${config.api.baseUrl}${post.imageUrl}`}
                      alt="Original pulse attachment"
                      className="w-full object-contain max-h-48"
                    />
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Unified Composer in repulse mode */}
          <UnifiedComposer
            mode="repulse"
            repulsedPulseId={post.id}
            onSuccess={handleSuccess}
            placeholder="Add your thoughts..."
            rows={4}
            focusedRows={4}
          />
        </div>
      </div>
    </div>
  );
}
