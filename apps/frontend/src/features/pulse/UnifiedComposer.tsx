import { useRef, useState, useEffect } from "react";
import { useAuth } from "../../hooks/useAuth";
import { buildAvatarUrl } from "../../utils/avatarUtils";
import { useHandleSearch } from "./useHandleSearch";
import { useHashtagSearch } from "./useHashtagSearch";
import { MentionAutocomplete } from "./MentionAutocomplete";
import { HashtagAutocomplete } from "./HashtagAutocomplete";
import { HighlightedTextarea } from "./HighlightedTextarea";
import { useCreatePulse } from "./hooks/useCreatePulse";
import { useCreateReply } from "./hooks/useCreateReply";
import { useCreateRepulse } from "./hooks/useCreateRepulse";

export interface UnifiedComposerProps {
  mode: "post" | "reply" | "repulse";
  parentPulseId?: string;
  repulsedPulseId?: string;
  onSuccess?: () => void;
  placeholder?: string;
  rows?: number;
  focusedRows?: number;
}

export function UnifiedComposer({
  mode,
  parentPulseId,
  repulsedPulseId,
  onSuccess,
  placeholder = "What's happening?",
  rows = 3,
  focusedRows = 8,
}: UnifiedComposerProps): JSX.Element {
  const { user } = useAuth();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // State
  const [value, setValue] = useState("");
  const [selectedImage, setSelectedImage] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [isFocused, setIsFocused] = useState(false);
  const [showMentions, setShowMentions] = useState(false);
  const [mentionPosition, setMentionPosition] = useState({ top: 0, left: 0 });
  const [showHashtags, setShowHashtags] = useState(false);
  const [hashtagPosition, setHashtagPosition] = useState({ top: 0, left: 0 });

  // Hooks
  const { results: mentionResults, search: searchMentions, clearResults: clearMentionResults } = useHandleSearch();
  const { results: hashtagResults, search: searchHashtags, clearResults: clearHashtagResults } = useHashtagSearch();
  const { createPulse, isCreating: isCreatingPulse } = useCreatePulse();
  const { createReply, isCreating: isCreatingReply, error: replyError } = useCreateReply();
  const { createRepulse, isCreating: isCreatingRepulse, error: repulseError } = useCreateRepulse();

  const maxChars = 300;
  const charsLeft = maxChars - value.length;
  const isOverLimit = charsLeft < 0;
  const isSubmitting = mode === "post" ? isCreatingPulse : mode === "reply" ? isCreatingReply : isCreatingRepulse;

  // Clear preview when selectedImage is cleared
  useEffect(() => {
    if (selectedImage === null) {
      setImagePreview(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
    }
  }, [selectedImage]);

  // Detect mentions and hashtags, trigger autocomplete
  useEffect(() => {
    const cursorPosition = textareaRef.current?.selectionStart ?? 0;
    const textBeforeCursor = value.slice(0, cursorPosition);

    // Check for mention (@)
    const mentionMatch = textBeforeCursor.match(/@(\w*)$/);
    // Check for hashtag (#)
    const hashtagMatch = textBeforeCursor.match(/#(\w*)$/);

    // Calculate position helper function
    const calculatePosition = () => {
      if (textareaRef.current) {
        const textarea = textareaRef.current;
        const rect = textarea.getBoundingClientRect();
        const lines = textBeforeCursor.split('\n');
        const currentLineIndex = lines.length - 1;
        const lineHeight = 24;
        const topOffset = currentLineIndex * lineHeight + lineHeight;

        return {
          top: rect.top + topOffset + 8,
          left: rect.left + 16,
        };
      }
      return { top: 0, left: 0 };
    };

    // Handle mention autocomplete
    if (mentionMatch && mentionMatch[1] !== undefined) {
      const searchTerm = mentionMatch[1];
      searchMentions(searchTerm);
      setShowMentions(true);
      setShowHashtags(false);
      setMentionPosition(calculatePosition());
    }
    // Handle hashtag autocomplete
    else if (hashtagMatch && hashtagMatch[1] !== undefined) {
      const searchTerm = hashtagMatch[1];
      searchHashtags(searchTerm);
      setShowHashtags(true);
      setShowMentions(false);
      setHashtagPosition(calculatePosition());
    }
    // Clear autocomplete if neither match
    else {
      setShowMentions(false);
      setShowHashtags(false);
      clearMentionResults();
      clearHashtagResults();
    }
  }, [value, searchMentions, searchHashtags, clearMentionResults, clearHashtagResults]);

  const handleSubmit = async (): Promise<void> => {
    if (value.trim().length === 0 || isOverLimit || isSubmitting) {
      return;
    }

    try {
      if (mode === "post") {
        await createPulse(value, selectedImage);
      } else if (mode === "reply" && parentPulseId) {
        await createReply(parentPulseId, value, selectedImage);
      } else if (mode === "repulse" && repulsedPulseId) {
        await createRepulse(repulsedPulseId, value, selectedImage);
      }

      // Clear form on success
      setValue("");
      setSelectedImage(null);
      setImagePreview(null);
      onSuccess?.();
    } catch (err) {
      // Error is handled by the hooks
      console.error(`Failed to ${mode}:`, err);
    }
  };

  const handleImageButtonClick = (): void => {
    fileInputRef.current?.click();
  };

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>): void => {
    const file = e.target.files?.[0];
    if (file && file.type.startsWith("image/")) {
      setSelectedImage(file);

      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        setImagePreview(e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleRemoveImage = (): void => {
    setSelectedImage(null);
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
      setValue(newText);

      // Move cursor after the mention
      setTimeout(() => {
        const newCursorPos = mentionStart + handle.length + 2; // +2 for @ and space
        textareaRef.current?.setSelectionRange(newCursorPos, newCursorPos);
        textareaRef.current?.focus();
      }, 0);
    }

    setShowMentions(false);
    clearMentionResults();
  };

  const handleHashtagSelect = (tag: string): void => {
    const cursorPosition = textareaRef.current?.selectionStart ?? 0;
    const textBeforeCursor = value.slice(0, cursorPosition);
    const textAfterCursor = value.slice(cursorPosition);

    // Replace the partial hashtag with the full tag
    const hashtagMatch = textBeforeCursor.match(/#(\w*)$/);
    if (hashtagMatch) {
      const hashtagStart = textBeforeCursor.lastIndexOf("#");
      const newText = textBeforeCursor.slice(0, hashtagStart) + `#${tag} ` + textAfterCursor;
      setValue(newText);

      // Move cursor after the hashtag
      setTimeout(() => {
        const newCursorPos = hashtagStart + tag.length + 2; // +2 for # and space
        textareaRef.current?.setSelectionRange(newCursorPos, newCursorPos);
        textareaRef.current?.focus();
      }, 0);
    }

    setShowHashtags(false);
    clearHashtagResults();
  };

  const avatarUrl = buildAvatarUrl(user?.avatarUrl);

  const buttonText = mode === "post" ? "Post" : mode === "reply" ? "Reply" : "Repulse";
  const submittingText = mode === "post" ? "Posting..." : mode === "reply" ? "Replying..." : "Repulsing...";

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
        <HighlightedTextarea
          ref={textareaRef}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onFocus={() => setIsFocused(true)}
          onBlur={() => setIsFocused(false)}
          rows={isFocused || value.length > 0 ? focusedRows : rows}
          placeholder={placeholder}
          className="w-full resize-none text-lg text-text-primary placeholder:text-text-tertiary focus:outline-none transition-all custom-scrollbar"
        />
        {showMentions && mentionResults.length > 0 && (
          <MentionAutocomplete
            results={mentionResults}
            onSelect={handleMentionSelect}
            position={mentionPosition}
          />
        )}
        {showHashtags && hashtagResults.length > 0 && (
          <HashtagAutocomplete
            results={hashtagResults}
            onSelect={handleHashtagSelect}
            position={hashtagPosition}
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
        {mode === "reply" && replyError && (
          <div className="text-sm text-red-400">{replyError}</div>
        )}
        {mode === "repulse" && repulseError && (
          <div className="text-sm text-red-400">{repulseError}</div>
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
              {isSubmitting ? submittingText : buttonText}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
