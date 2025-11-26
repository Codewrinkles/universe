import type { PostVideo as PostVideoType } from "../../types";

interface PostVideoProps {
  video: PostVideoType;
}

export function PostVideo({ video }: PostVideoProps): JSX.Element {
  return (
    <div className="mt-3 relative overflow-hidden rounded-2xl border border-border bg-black">
      {/* Thumbnail or placeholder */}
      {video.thumbnailUrl ? (
        <img
          src={video.thumbnailUrl}
          alt="Video thumbnail"
          className="w-full h-auto max-h-[400px] object-cover"
        />
      ) : (
        <div className="w-full h-64 bg-gradient-to-br from-surface-card1 to-surface-card2 flex items-center justify-center">
          <span className="text-4xl">🎬</span>
        </div>
      )}

      {/* Play button overlay */}
      <button
        type="button"
        className="absolute inset-0 flex items-center justify-center bg-black/20 hover:bg-black/30 transition-colors group"
      >
        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-brand-soft/90 group-hover:bg-brand-soft group-hover:scale-105 transition-all shadow-lg">
          <svg
            className="h-7 w-7 text-black ml-1"
            fill="currentColor"
            viewBox="0 0 24 24"
          >
            <path d="M8 5v14l11-7z" />
          </svg>
        </div>
      </button>

      {/* Duration badge */}
      {video.duration && (
        <div className="absolute bottom-3 right-3 rounded bg-black/80 px-2 py-1 text-xs font-medium text-white">
          {video.duration}
        </div>
      )}
    </div>
  );
}
