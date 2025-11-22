import type { Post } from "../../types";

export interface PostCardProps {
  post: Post;
}

function FeedAction({ icon, label }: { icon: string; label: string }): JSX.Element {
  return (
    <button
      type="button"
      className="inline-flex items-center gap-1 rounded-full px-2 py-1 text-xs text-text-tertiary hover:bg-surface-page hover:text-text-primary transition-colors"
    >
      <span className="text-[11px]">{icon}</span>
      <span>{label}</span>
    </button>
  );
}

export function PostCard({ post }: PostCardProps): JSX.Element {
  return (
    <article className="rounded-2xl border border-border bg-surface-card1 p-4 transition-all duration-150 hover:border-brand-soft/50 hover:bg-surface-card2">
      <header className="mb-2 flex items-start justify-between gap-3 text-sm">
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-surface-card2 text-xs font-semibold text-text-primary border border-border">
            D
          </div>
          <div className="flex flex-col">
            <span className="font-medium tracking-tight text-text-primary">
              Daniel @ Codewrinkles
            </span>
            <span className="text-xs text-text-tertiary">@codewrinkles</span>
          </div>
        </div>
        <span className="text-xs text-text-tertiary">{post.timeAgo}</span>
      </header>
      <p className="text-sm leading-relaxed text-text-primary mb-3">{post.content}</p>

      {post.type === "quote" && post.quoted && (
        <div className="mb-3 rounded-xl border border-border bg-surface-page/60 px-3 py-2 text-xs text-text-secondary">
          <div className="mb-1 text-[10px] uppercase tracking-wide text-text-tertiary">
            {post.quoted.author}
          </div>
          <div className="italic">&ldquo;{post.quoted.text}&rdquo;</div>
        </div>
      )}

      {post.type === "thread" && (
        <div className="mb-3 rounded-xl border border-dashed border-border bg-surface-page/50 px-3 py-2 text-xs text-text-tertiary">
          <div className="flex items-center justify-between">
            <span>Thread preview</span>
            <span className="text-[10px]">{post.repliesPreview} replies</span>
          </div>
          <div className="mt-1 h-[1px] bg-border-deep/70" />
        </div>
      )}

      {post.type === "image" && (
        <div className="mb-3 overflow-hidden rounded-xl border border-border bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 h-40 flex items-center justify-center text-[11px] text-text-tertiary">
          <span>Image placeholder – vertical slice diagram</span>
        </div>
      )}

      <footer className="mt-2 flex gap-4 text-xs text-text-tertiary">
        <FeedAction icon="💬" label="Reply" />
        <FeedAction icon="✧" label="Appreciate" />
        <FeedAction icon="↗" label="Share" />
      </footer>
    </article>
  );
}
