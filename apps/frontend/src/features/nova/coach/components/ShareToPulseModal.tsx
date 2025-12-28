import { useEffect, useState, useCallback } from "react";
import { useAuth } from "../../../../hooks/useAuth";
import { buildAvatarUrl } from "../../../../utils/avatarUtils";
import { novaApi } from "../../../../services/novaApi";
import { useCreatePulse } from "../../../pulse/hooks/useCreatePulse";

export interface ShareToPulseModalProps {
  isOpen: boolean;
  onClose: () => void;
  novaContent: string;
}

type ModalStatus = "loading" | "ready" | "posting" | "success" | "error";

const ATTRIBUTION = "\n\n💡 via #nova";
const MAX_CHARS = 500;
const MAX_SUMMARY_CHARS = 450; // Leave buffer, summaries should be complete thoughts

/**
 * ShareToPulseModal - Modal for sharing Nova responses to Pulse
 *
 * Features:
 * - Fetches AI-generated summary on open
 * - Editable summary with character counter
 * - Attribution automatically appended
 * - Posts to Pulse using existing hook
 * - Success toast and auto-close
 */
export function ShareToPulseModal({
  isOpen,
  onClose,
  novaContent,
}: ShareToPulseModalProps): JSX.Element | null {
  const { user } = useAuth();
  const { createPulse, isCreating } = useCreatePulse();

  const [status, setStatus] = useState<ModalStatus>("loading");
  const [summary, setSummary] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [showSuccessToast, setShowSuccessToast] = useState(false);

  // Fetch summary when modal opens
  const fetchSummary = useCallback(async (): Promise<void> => {
    setStatus("loading");
    setError(null);

    try {
      const response = await novaApi.summarizeForPulse(novaContent);
      setSummary(response.summary);
      setStatus("ready");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to summarize");
      setStatus("error");
    }
  }, [novaContent]);

  useEffect(() => {
    if (isOpen) {
      void fetchSummary();
    } else {
      // Reset state when closed
      setSummary("");
      setError(null);
      setStatus("loading");
      setShowSuccessToast(false);
    }
  }, [isOpen, fetchSummary]);

  // Handle ESC key to close modal
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent): void => {
      if (e.key === "Escape" && isOpen && status !== "posting") {
        onClose();
      }
    };

    document.addEventListener("keydown", handleEscape);
    return () => document.removeEventListener("keydown", handleEscape);
  }, [isOpen, onClose, status]);

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

  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>): void => {
    if (e.target === e.currentTarget && status !== "posting") {
      onClose();
    }
  };

  const handleShare = async (): Promise<void> => {
    if (status === "posting" || isCreating) return;

    setStatus("posting");
    setError(null);

    try {
      // Combine summary with attribution
      const fullContent = summary.trim() + ATTRIBUTION;
      await createPulse(fullContent);

      setStatus("success");
      setShowSuccessToast(true);

      // Auto-close after showing success
      setTimeout(() => {
        onClose();
      }, 1500);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to post");
      setStatus("error");
    }
  };

  // Calculate character counts
  const fullLength = summary.length + ATTRIBUTION.length;
  const charsLeft = MAX_CHARS - fullLength;
  const isOverLimit = charsLeft < 0;

  const avatarUrl = buildAvatarUrl(user?.avatarUrl);

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center bg-black/60 backdrop-blur-sm p-4 overflow-y-auto"
      onClick={handleBackdropClick}
      role="dialog"
      aria-modal="true"
      aria-labelledby="share-modal-title"
    >
      <div className="w-full max-w-[600px] bg-surface-card1 border border-border rounded-2xl shadow-2xl mt-12 mb-4 animate-slide-up">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-border px-4 py-3">
          <h2
            id="share-modal-title"
            className="text-base font-semibold text-text-primary"
          >
            Share to Pulse
          </h2>
          <button
            type="button"
            onClick={onClose}
            disabled={status === "posting"}
            className="rounded-full p-2 text-text-secondary hover:bg-surface-card2 hover:text-text-primary transition-colors disabled:opacity-50"
            aria-label="Close"
          >
            ✕
          </button>
        </div>

        {/* Content */}
        <div className="p-4">
          {/* Loading State */}
          {status === "loading" && (
            <div className="flex items-center justify-center py-12">
              <div className="flex items-center gap-3 text-text-secondary">
                <div className="animate-spin h-5 w-5 border-2 border-violet-500 border-t-transparent rounded-full" />
                <span className="text-sm">Summarizing...</span>
              </div>
            </div>
          )}

          {/* Error State */}
          {status === "error" && (
            <div className="py-8 text-center">
              <p className="text-red-400 text-sm mb-4">{error}</p>
              <button
                type="button"
                onClick={() => void fetchSummary()}
                className="px-4 py-2 text-sm font-medium text-violet-400 hover:text-violet-300 transition-colors"
              >
                Try again
              </button>
            </div>
          )}

          {/* Ready/Posting State */}
          {(status === "ready" || status === "posting") && (
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
              <div className="flex-1 space-y-3">
                <div className="relative">
                  {/* Editable summary area */}
                  <div className="rounded-xl bg-surface-card2 border border-border p-3">
                    <textarea
                      value={summary}
                      onChange={(e) => setSummary(e.target.value)}
                      disabled={status === "posting"}
                      rows={5}
                      maxLength={MAX_SUMMARY_CHARS + 50} // Allow some overflow to show error
                      className="w-full resize-none bg-transparent text-sm text-text-primary placeholder:text-text-tertiary focus:outline-none disabled:opacity-50"
                      placeholder="Your summary..."
                    />
                    {/* Attribution - shown as part of the content */}
                    <div className="text-sm text-text-primary pt-1 border-t border-border/50 mt-2">
                      💡 via <span className="text-brand-soft">#nova</span>
                    </div>
                  </div>
                  <p className="text-[11px] text-text-tertiary mt-2">
                    The attribution above will be included in your Pulse post.
                  </p>
                </div>

                {/* Footer */}
                <div className="flex items-center justify-between border-t border-border pt-3">
                  <div />
                  <div className="flex items-center gap-3">
                    {/* Character counter */}
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
                          strokeDasharray={`${Math.min((fullLength / MAX_CHARS) * 50.27, 50.27)} 50.27`}
                          className={
                            isOverLimit
                              ? "text-red-500"
                              : charsLeft <= 20
                                ? "text-amber-500"
                                : "text-brand-soft"
                          }
                        />
                      </svg>
                    </div>
                    {charsLeft <= 50 && (
                      <span
                        className={`text-xs tabular-nums ${isOverLimit ? "text-red-400" : charsLeft <= 20 ? "text-amber-400" : "text-text-tertiary"}`}
                      >
                        {charsLeft}
                      </span>
                    )}
                    <div className="h-6 w-px bg-border" />
                    <button
                      type="button"
                      disabled={
                        summary.trim().length === 0 ||
                        isOverLimit ||
                        status === "posting"
                      }
                      onClick={() => void handleShare()}
                      className={`rounded-full px-4 py-1.5 text-sm font-semibold transition-colors ${
                        summary.trim().length === 0 ||
                        isOverLimit ||
                        status === "posting"
                          ? "bg-brand-soft/50 text-black/50 cursor-not-allowed"
                          : "bg-brand-soft text-black hover:bg-brand"
                      }`}
                    >
                      {status === "posting" ? "Sharing..." : "Share"}
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Success State */}
          {status === "success" && showSuccessToast && (
            <div className="py-8 text-center">
              <div className="inline-flex items-center gap-2 px-4 py-2 bg-green-500/20 border border-green-500/40 rounded-full text-green-400 text-sm">
                <svg
                  className="w-4 h-4"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M5 13l4 4L19 7"
                  />
                </svg>
                Shared to Pulse!
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
