import type { Hashtag } from "../../types";

export interface HashtagAutocompleteProps {
  results: Hashtag[];
  onSelect: (tag: string) => void;
  position: { top: number; left: number };
}

export function HashtagAutocomplete({
  results,
  onSelect,
  position,
}: HashtagAutocompleteProps): JSX.Element {
  if (results.length === 0) {
    return <></>;
  }

  const formatCount = (count: number): string => {
    if (count >= 1000) {
      return `${(count / 1000).toFixed(1)}K`;
    }
    return count.toString();
  };

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
        {results.map((hashtag) => (
          <button
            key={hashtag.id}
            type="button"
            onClick={() => onSelect(hashtag.tagDisplay)}
            className="w-full px-4 py-2 flex items-center justify-between hover:bg-surface-card2 transition-colors text-left"
          >
            <div className="flex flex-col min-w-0">
              <span className="text-sm font-medium text-text-primary">
                #{hashtag.tagDisplay}
              </span>
              <span className="text-xs text-text-secondary">
                {formatCount(hashtag.pulseCount)} {hashtag.pulseCount === 1 ? "pulse" : "pulses"}
              </span>
            </div>
            <span className="text-lg ml-2">#️⃣</span>
          </button>
        ))}
      </div>
    </div>
  );
}
