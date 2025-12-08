import { useRef, useEffect, forwardRef } from "react";

export interface HighlightedTextareaProps {
  value: string;
  onChange: (e: React.ChangeEvent<HTMLTextAreaElement>) => void;
  onFocus?: () => void;
  onBlur?: () => void;
  placeholder?: string;
  rows?: number;
  className?: string;
}

export const HighlightedTextarea = forwardRef<HTMLTextAreaElement, HighlightedTextareaProps>(
  ({ value, onChange, onFocus, onBlur, placeholder, rows, className }, ref) => {
    const highlightRef = useRef<HTMLDivElement>(null);
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    // Sync scroll between textarea and highlight layer
    const handleScroll = (): void => {
      if (textareaRef.current && highlightRef.current) {
        highlightRef.current.scrollTop = textareaRef.current.scrollTop;
        highlightRef.current.scrollLeft = textareaRef.current.scrollLeft;
      }
    };

    // Expose textarea ref to parent
    useEffect(() => {
      if (typeof ref === "function") {
        ref(textareaRef.current);
      } else if (ref) {
        ref.current = textareaRef.current;
      }
    }, [ref]);

    // Generate highlighted HTML with mentions and hashtags colored
    const getHighlightedContent = (): string => {
      if (!value) {
        return "";
      }

      // Escape HTML and replace mentions and hashtags with colored spans
      // Uses CSS variable --color-brand-link which adapts to light/dark theme
      // Note: [\w-] for mentions aligns with handle validation which allows hyphens
      return value
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/(@[\w-]{3,30})/g, '<span style="color: var(--color-brand-link)">$1</span>')
        .replace(/(#\w{2,100})/g, '<span style="color: var(--color-brand-link)">$1</span>')
        .replace(/\n/g, "<br/>");
    };

    return (
      <div className="relative">
        {/* Actual textarea (bottom layer, normal theme colors) */}
        <textarea
          ref={textareaRef}
          value={value}
          onChange={onChange}
          onFocus={onFocus}
          onBlur={onBlur}
          onScroll={handleScroll}
          rows={rows}
          placeholder={placeholder}
          className={`relative z-0 p-0 border-0 bg-transparent caret-text-primary ${className}`}
        />

        {/* Highlight layer (top layer, only mentions/hashtags colored) */}
        <div
          ref={highlightRef}
          className={`absolute inset-0 z-10 p-0 pointer-events-none overflow-auto whitespace-pre-wrap break-words ${className}`}
          style={{
            color: "transparent",
            background: "transparent",
            border: "none",
          }}
          dangerouslySetInnerHTML={{ __html: getHighlightedContent() }}
        />
      </div>
    );
  }
);

HighlightedTextarea.displayName = "HighlightedTextarea";
