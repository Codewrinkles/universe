import type { HandleSearchResult } from "../../types";

export interface MentionAutocompleteProps {
  results: HandleSearchResult[];
  onSelect: (handle: string) => void;
  position: { top: number; left: number };
}

export function MentionAutocomplete({
  results,
  onSelect,
  position,
}: MentionAutocompleteProps): JSX.Element {
  if (results.length === 0) {
    return <></>;
  }

  return (
    <div
      className="fixed z-50 bg-surface-card1 border border-border rounded-xl shadow-lg overflow-hidden"
      style={{
        top: `${position.top}px`,
        left: `${position.left}px`,
        minWidth: "200px",
        maxWidth: "300px",
      }}
    >
      <div className="py-2">
        {results.map((result) => (
          <button
            key={result.profileId}
            type="button"
            onClick={() => onSelect(result.handle)}
            className="w-full px-4 py-2 flex items-center gap-3 hover:bg-surface-card2 transition-colors text-left"
          >
            {result.avatarUrl ? (
              <img
                src={result.avatarUrl}
                alt={result.name}
                className="w-8 h-8 rounded-full"
              />
            ) : (
              <div className="w-8 h-8 rounded-full bg-brand flex items-center justify-center text-white text-sm font-semibold">
                {result.name.charAt(0).toUpperCase()}
              </div>
            )}
            <div className="flex flex-col min-w-0">
              <span className="text-sm font-medium text-text-primary truncate">
                {result.name}
              </span>
              <span className="text-xs text-text-secondary truncate">
                @{result.handle}
              </span>
            </div>
          </button>
        ))}
      </div>
    </div>
  );
}
