import { useEffect } from "react";
import { UnifiedComposer } from "./UnifiedComposer";

export interface ComposerModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess?: () => void;
}

/**
 * Modal overlay for creating pulses
 * Opens when user clicks the "Post" button in navigation
 * Follows Twitter/X UX pattern for familiarity
 */
export function ComposerModal({ isOpen, onClose, onSuccess }: ComposerModalProps): JSX.Element | null {
  // Handle ESC key to close modal
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent): void => {
      if (e.key === "Escape" && isOpen) {
        onClose();
      }
    };

    document.addEventListener("keydown", handleEscape);
    return () => document.removeEventListener("keydown", handleEscape);
  }, [isOpen, onClose]);

  // Prevent body scroll when modal is open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }

    return () => {
      document.body.style.overflow = "";
    };
  }, [isOpen]);

  if (!isOpen) {
    return null;
  }

  const handleSuccess = (): void => {
    onSuccess?.();
    onClose();
  };

  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>): void => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center bg-black/60 backdrop-blur-sm p-4 overflow-y-auto"
      onClick={handleBackdropClick}
      role="dialog"
      aria-modal="true"
      aria-labelledby="composer-modal-title"
    >
      <div className="w-full max-w-[600px] bg-surface-card1 border border-border rounded-2xl shadow-2xl mt-12 mb-4 animate-slide-up">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-border px-4 py-3">
          <h2 id="composer-modal-title" className="text-base font-semibold text-text-primary">
            Create a pulse
          </h2>
          <button
            type="button"
            onClick={onClose}
            className="rounded-full p-2 text-text-secondary hover:bg-surface-card2 hover:text-text-primary transition-colors"
            aria-label="Close composer"
          >
            ✕
          </button>
        </div>

        {/* Composer */}
        <div className="p-4">
          <UnifiedComposer
            mode="post"
            onSuccess={handleSuccess}
            placeholder="What's happening?"
            rows={4}
            focusedRows={10}
          />
        </div>
      </div>
    </div>
  );
}
