import { useRef, useState, useEffect } from "react";
import { useAuth } from "../../hooks/useAuth";
import { buildAvatarUrl } from "../../utils/avatarUtils";
import { useHandleSearch } from "./useHandleSearch";
import { MentionAutocomplete } from "./MentionAutocomplete";

export interface ComposerProps {
  value: string;
  onChange: (value: string) => void;
  maxChars: number;
  isOverLimit: boolean;
  charsLeft: number;
  onSubmit?: () => void;
  isSubmitting?: boolean;
  selectedImage?: File | null;
  onImageSelect?: (file: File | null) => void;
}

export function Composer({ value, onChange, maxChars, isOverLimit, charsLeft, onSubmit, isSubmitting = false, selectedImage, onImageSelect }: ComposerProps): JSX.Element {
  const { user } = useAuth();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [isFocused, setIsFocused] = useState(false);
  const [showMentions, setShowMentions] = useState(false);
  const [mentionPosition, setMentionPosition] = useState({ top: 0, left: 0 });
  const { results, search, clearResults } = useHandleSearch();

  // Clear preview when selectedImage is cleared by parent
  useEffect(() => {
    if (selectedImage === null) {
      setImagePreview(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
    }
  }, [selectedImage]);

  // Detect mentions and trigger autocomplete
  useEffect(() => {
    const cursorPosition = textareaRef.current?.selectionStart ?? 0;
    const textBeforeCursor = value.slice(0, cursorPosition);
    const mentionMatch = textBeforeCursor.match(/@(\w*)$/);

    if (mentionMatch && mentionMatch[1] !== undefined) {
      const searchTerm = mentionMatch[1];
      search(searchTerm);
      setShowMentions(true);

      // Calculate autocomplete position near the cursor
      if (textareaRef.current) {
        const textarea = textareaRef.current;
        const rect = textarea.getBoundingClientRect();

        // Get the number of newlines before cursor to estimate line position
        const textBeforeCursor = value.slice(0, cursorPosition);
        const lines = textBeforeCursor.split('\n');
        const currentLineIndex = lines.length - 1;
        const lineHeight = 24; // Approximate line height in pixels

        // Position below the current line
        const topOffset = currentLineIndex * lineHeight + lineHeight;

        setMentionPosition({
          top: rect.top + topOffset + 8,
          left: rect.left + 16, // Add some left padding
        });
      }
    } else {
      setShowMentions(false);
      clearResults();
    }
  }, [value, search, clearResults]);

  const handleSubmit = (): void => {
    if (value.trim().length === 0 || isOverLimit || isSubmitting) {
      return;
    }
    onSubmit?.();
  };

  const handleImageButtonClick = (): void => {
    fileInputRef.current?.click();
  };

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>): void => {
    const file = e.target.files?.[0];
    if (file && file.type.startsWith("image/")) {
      onImageSelect?.(file);

      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        setImagePreview(e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleRemoveImage = (): void => {
    onImageSelect?.(null);
    setImagePreview(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const handleMentionSelect = (handle: string): void => {
    const cursorPosition = textareaRef.current?.selectionStart ?? 0;
    const textBeforeCursor = value.slice(0, cursorPosition);
    const textAfterCursor = value.slice(cursorPosition);

    // Replace the partial mention with the full handle
    const mentionMatch = textBeforeCursor.match(/@(\w*)$/);
    if (mentionMatch) {
      const mentionStart = textBeforeCursor.lastIndexOf("@");
      const newText = textBeforeCursor.slice(0, mentionStart) + `@${handle} ` + textAfterCursor;
      onChange(newText);

      // Move cursor after the mention
      setTimeout(() => {
        const newCursorPos = mentionStart + handle.length + 2; // +2 for @ and space
        textareaRef.current?.setSelectionRange(newCursorPos, newCursorPos);
        textareaRef.current?.focus();
      }, 0);
    }

    setShowMentions(false);
    clearResults();
  };

  const avatarUrl = buildAvatarUrl(user?.avatarUrl);

  return (
    <div className="flex gap-3">
      {avatarUrl ? (
        <img
          src={avatarUrl}
          alt={user?.name ?? "Profile"}
          className="h-10 w-10 flex-shrink-0 rounded-full object-cover"
        />
      ) : (
        <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full border border-border bg-surface-card2 text-sm font-semibold text-text-primary">
          {user?.name?.charAt(0).toUpperCase() ?? "?"}
        </div>
      )}
      <div className="flex-1 space-y-3 relative">
        <textarea
          ref={textareaRef}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onFocus={() => setIsFocused(true)}
          onBlur={() => setIsFocused(false)}
          rows={isFocused || value.length > 0 ? 8 : 3}
          placeholder="What's happening?"
          className="w-full resize-none bg-transparent text-lg text-text-primary placeholder:text-text-tertiary focus:outline-none transition-all custom-scrollbar"
        />
        {showMentions && results.length > 0 && (
          <MentionAutocomplete
            results={results}
            onSelect={handleMentionSelect}
            position={mentionPosition}
          />
        )}
        {imagePreview && (
          <div className="relative inline-block rounded-xl overflow-hidden border border-border">
            <img
              src={imagePreview}
              alt="Selected"
              className="max-h-96 rounded-xl object-contain"
            />
            <button
              type="button"
              onClick={handleRemoveImage}
              className="absolute top-2 right-2 rounded-full bg-black/70 p-1.5 text-white hover:bg-black/90 transition-colors"
              title="Remove image"
            >
              ✕
            </button>
          </div>
        )}
        <div className="flex items-center justify-between border-t border-border pt-3">
          <div className="flex items-center gap-1">
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              onChange={handleImageChange}
              className="hidden"
            />
            <button
              type="button"
              onClick={handleImageButtonClick}
              className="rounded-full p-2 text-brand-soft hover:bg-brand-soft/10 transition-colors"
              title="Add image"
            >
              🖼️
            </button>
            <button
              type="button"
              className="rounded-full p-2 text-brand-soft hover:bg-brand-soft/10 transition-colors"
              title="Add GIF"
            >
              📷
            </button>
            <button
              type="button"
              className="rounded-full p-2 text-brand-soft hover:bg-brand-soft/10 transition-colors"
              title="Add poll"
            >
              📊
            </button>
            <button
              type="button"
              className="rounded-full p-2 text-brand-soft hover:bg-brand-soft/10 transition-colors"
              title="Add emoji"
            >
              😊
            </button>
          </div>
          <div className="flex items-center gap-3">
            {value.length > 0 && (
              <>
                <div className="h-5 w-5 relative">
                  <svg className="h-5 w-5 -rotate-90" viewBox="0 0 20 20">
                    <circle
                      cx="10"
                      cy="10"
                      r="8"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      className="text-border"
                    />
                    <circle
                      cx="10"
                      cy="10"
                      r="8"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      strokeDasharray={`${Math.min((value.length / maxChars) * 50.27, 50.27)} 50.27`}
                      className={isOverLimit ? "text-red-500" : charsLeft <= 20 ? "text-amber-500" : "text-brand-soft"}
                    />
                  </svg>
                </div>
                {charsLeft <= 20 && (
                  <span className={`text-xs tabular-nums ${isOverLimit ? "text-red-400" : "text-amber-400"}`}>
                    {charsLeft}
                  </span>
                )}
                <div className="h-6 w-px bg-border" />
              </>
            )}
            <button
              type="button"
              disabled={value.trim().length === 0 || isOverLimit || isSubmitting}
              onClick={handleSubmit}
              className={`rounded-full px-4 py-1.5 text-sm font-semibold transition-colors ${
                value.trim().length === 0 || isOverLimit || isSubmitting
                  ? "bg-brand-soft/50 text-black/50 cursor-not-allowed"
                  : "bg-brand-soft text-black hover:bg-brand"
              }`}
            >
              {isSubmitting ? "Posting..." : "Post"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
