import { useState, useRef, useEffect } from "react";

export interface FeedFilterControlProps {
  hideReplies: boolean;
  onToggleReplies: (hide: boolean) => void;
}

export function FeedFilterControl({
  hideReplies,
  onToggleReplies,
}: FeedFilterControlProps): JSX.Element {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent): void => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node) &&
        buttonRef.current &&
        !buttonRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside);
    }

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [isOpen]);

  // Handle escape key to close dropdown
  useEffect(() => {
    const handleEscape = (event: KeyboardEvent): void => {
      if (event.key === "Escape" && isOpen) {
        setIsOpen(false);
        buttonRef.current?.focus();
      }
    };

    if (isOpen) {
      document.addEventListener("keydown", handleEscape);
    }

    return () => {
      document.removeEventListener("keydown", handleEscape);
    };
  }, [isOpen]);

  const activeFilterCount = hideReplies ? 1 : 0;

  return (
    <div className="relative">
      <button
        ref={buttonRef}
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 px-3 py-2 bg-surface-card1 border border-border rounded-full text-text-secondary hover:text-text-primary hover:border-brand-DEFAULT transition-colors"
        aria-label="Filter feed"
        aria-expanded={isOpen}
        aria-haspopup="true"
      >
        <svg
          className="w-4 h-4"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z"
          />
        </svg>
        <span className="text-sm font-medium hidden sm:inline">Filter</span>
        {activeFilterCount > 0 && (
          <span className="flex items-center justify-center w-5 h-5 bg-brand-DEFAULT text-white text-[11px] font-semibold rounded-full">
            {activeFilterCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div
          ref={dropdownRef}
          className="absolute right-0 mt-2 w-64 bg-surface-card2 border border-border rounded-xl shadow-lg z-50"
          role="menu"
          aria-orientation="vertical"
        >
          <div className="p-4 space-y-3">
            <div className="flex items-center justify-between border-b border-border pb-3">
              <h3 className="text-sm font-semibold text-text-primary">
                Feed Filters
              </h3>
              {activeFilterCount > 0 && (
                <button
                  onClick={() => onToggleReplies(false)}
                  className="text-xs text-brand-DEFAULT hover:text-brand-soft transition-colors"
                  aria-label="Clear all filters"
                >
                  Clear all
                </button>
              )}
            </div>

            <div className="space-y-3">
              <label className="flex items-center justify-between cursor-pointer group">
                <div className="flex-1">
                  <div className="text-sm font-medium text-text-primary group-hover:text-brand-DEFAULT transition-colors">
                    Hide replies
                  </div>
                  <div className="text-xs text-text-tertiary mt-0.5">
                    Show only original posts and repulses
                  </div>
                </div>
                <button
                  role="switch"
                  aria-checked={hideReplies}
                  aria-label="Hide replies from feed"
                  onClick={() => onToggleReplies(!hideReplies)}
                  className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-brand-DEFAULT focus:ring-offset-2 focus:ring-offset-surface-card2 ml-3 ${
                    hideReplies ? "bg-brand-soft" : "bg-gray-300 dark:bg-gray-700"
                  }`}
                >
                  <span
                    className={`inline-block h-4 w-4 transform rounded-full bg-white shadow-lg ring-1 ring-black/10 transition-transform duration-200 ease-in-out ${
                      hideReplies ? "translate-x-6" : "translate-x-1"
                    }`}
                  />
                </button>
              </label>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
