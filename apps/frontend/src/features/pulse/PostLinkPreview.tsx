import type { PulseLinkPreview } from "../../types";

// Legacy type alias
type PostLinkPreviewType = PulseLinkPreview;

interface PostLinkPreviewProps {
  link: PostLinkPreviewType;
}

export function PostLinkPreview({ link }: PostLinkPreviewProps): JSX.Element {
  return (
    <a
      href={link.url}
      target="_blank"
      rel="noopener noreferrer"
      className="mt-3 block overflow-hidden rounded-2xl border border-border hover:bg-surface-card1 transition-colors"
    >
      {/* Link image */}
      {link.imageUrl && (
        <div className="border-b border-border bg-black/5 dark:bg-black/20 flex items-center justify-center">
          <img
            src={link.imageUrl}
            alt={link.title}
            className="w-full max-h-80 object-contain"
          />
        </div>
      )}

      {/* Link info */}
      <div className="p-3">
        <p className="text-xs text-text-tertiary flex items-center gap-1">
          <span>🔗</span>
          <span>{link.domain}</span>
        </p>
        <h4 className="mt-1 text-sm font-medium text-text-primary line-clamp-2">
          {link.title}
        </h4>
        {link.description && (
          <p className="mt-1 text-xs text-text-secondary line-clamp-2">
            {link.description}
          </p>
        )}
      </div>
    </a>
  );
}
