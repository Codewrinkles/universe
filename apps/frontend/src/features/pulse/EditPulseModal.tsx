import { useState, useEffect, useRef } from "react";
import type { Post } from "../../types";
import { HighlightedTextarea } from "./HighlightedTextarea";
import { useEditPulse } from "./hooks/useEditPulse";

const MAX_CONTENT_LENGTH = 500;

export interface EditPulseModalProps {
  post: Post;
  onSave: () => void;
  onCancel: () => void;
}

export function EditPulseModal({ post, onSave, onCancel }: EditPulseModalProps): JSX.Element {
  const [content, setContent] = useState(post.content);
  const { editPulse, isEditing } = useEditPulse();
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Focus textarea on mount
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.focus();
      // Move cursor to end
      textareaRef.current.setSelectionRange(content.length, content.length);
    }
  }, []);

  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>): void => {
    if (e.target === e.currentTarget && !isEditing) {
      onCancel();
    }
  };

  const handleContentChange = (e: React.ChangeEvent<HTMLTextAreaElement>): void => {
    setContent(e.target.value);
  };

  const handleSave = async (): Promise<void> => {
    if (isEditing || !hasChanges || !isValidLength) return;

    try {
      await editPulse(post.id, content);
      onSave();
    } catch {
      // Error is handled by hook and displayed in UI
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent): void => {
    // Cmd/Ctrl + Enter to save
    if ((e.metaKey || e.ctrlKey) && e.key === "Enter") {
      e.preventDefault();
      handleSave();
    }
    // Escape to cancel
    if (e.key === "Escape" && !isEditing) {
      onCancel();
    }
  };

  const charCount = content.length;
  const isOverLimit = charCount > MAX_CONTENT_LENGTH;
  const isValidLength = charCount > 0 && charCount <= MAX_CONTENT_LENGTH;
  const hasChanges = content.trim() !== post.content.trim();
  const canSave = hasChanges && isValidLength && !isEditing;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70"
      onClick={handleBackdropClick}
      onKeyDown={handleKeyDown}
    >
      <div
        className="w-full max-w-[500px] bg-surface-card1 rounded-2xl border border-border mx-4"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between px-4 py-3 border-b border-border">
          <h2 className="text-base font-semibold text-text-primary">Edit Pulse</h2>
          <button
            type="button"
            onClick={onCancel}
            disabled={isEditing}
            className="text-text-secondary hover:text-text-primary transition-colors disabled:opacity-50"
            aria-label="Close"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div className="p-4">
          {/* Show image if present (non-editable) */}
          {post.imageUrl && (
            <div className="mb-4">
              <img
                src={post.imageUrl}
                alt="Pulse image"
                className="w-full rounded-xl object-cover max-h-64 opacity-75"
              />
              <p className="text-xs text-text-tertiary mt-1">Image cannot be changed</p>
            </div>
          )}

          {/* Textarea */}
          <HighlightedTextarea
            ref={textareaRef}
            value={content}
            onChange={handleContentChange}
            placeholder="What's happening?"
            rows={6}
            className="w-full resize-none text-text-primary text-sm focus:outline-none"
          />

          {/* Character count */}
          <div className="flex justify-end mt-2">
            <span
              className={`text-xs ${
                isOverLimit
                  ? "text-red-500"
                  : charCount > MAX_CONTENT_LENGTH * 0.9
                    ? "text-yellow-500"
                    : "text-text-tertiary"
              }`}
            >
              {charCount}/{MAX_CONTENT_LENGTH}
            </span>
          </div>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end gap-2 px-4 py-3 border-t border-border">
          <button
            type="button"
            onClick={onCancel}
            disabled={isEditing}
            className="px-4 py-2 rounded-full border border-border bg-transparent text-sm font-semibold text-text-primary hover:bg-surface-card2 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={!canSave}
            className="px-4 py-2 rounded-full bg-brand text-sm font-semibold text-white hover:bg-brand-soft transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isEditing ? "Saving..." : "Save"}
          </button>
        </div>
      </div>
    </div>
  );
}
