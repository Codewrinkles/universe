export interface DeleteConfirmDialogProps {
  onConfirm: () => void;
  onCancel: () => void;
  isDeleting?: boolean;
}

export function DeleteConfirmDialog({ onConfirm, onCancel, isDeleting = false }: DeleteConfirmDialogProps): JSX.Element {
  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>): void => {
    if (e.target === e.currentTarget && !isDeleting) {
      onCancel();
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70"
      onClick={handleBackdropClick}
    >
      <div
        className="w-full max-w-[320px] bg-surface-card1 rounded-2xl border border-border mx-4"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Content */}
        <div className="p-6 space-y-4">
          <h2 className="text-lg font-semibold text-text-primary">
            Delete Pulse?
          </h2>
          <p className="text-sm text-text-secondary">
            This can't be undone and it will be removed from your profile and the feed.
          </p>

          {/* Buttons */}
          <div className="space-y-2">
            <button
              type="button"
              onClick={onConfirm}
              disabled={isDeleting}
              className="w-full rounded-full bg-red-500 px-4 py-2.5 text-sm font-semibold text-white hover:bg-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isDeleting ? "Deleting..." : "Delete"}
            </button>
            <button
              type="button"
              onClick={onCancel}
              disabled={isDeleting}
              className="w-full rounded-full border border-border bg-transparent px-4 py-2.5 text-sm font-semibold text-text-primary hover:bg-surface-card2 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
