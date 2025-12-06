import { useEffect } from "react";

export interface ImageGalleryOverlayProps {
  imageUrl: string;
  altText?: string;
  onClose: () => void;
}

export function ImageGalleryOverlay({
  imageUrl,
  altText = "Image",
  onClose,
}: ImageGalleryOverlayProps): JSX.Element {
  // Prevent body scroll when overlay is open
  useEffect(() => {
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = "";
    };
  }, []);

  // Close on ESC key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent): void => {
      if (e.key === "Escape") {
        onClose();
      }
    };

    document.addEventListener("keydown", handleEscape);
    return () => {
      document.removeEventListener("keydown", handleEscape);
    };
  }, [onClose]);

  // Handle backdrop click (close overlay)
  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>): void => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  // Prevent image click from closing overlay
  const handleImageClick = (e: React.MouseEvent): void => {
    e.stopPropagation();
  };

  return (
    <div
      className="fixed inset-0 z-50 bg-black/95 flex items-center justify-center"
      onClick={handleBackdropClick}
      role="dialog"
      aria-modal="true"
      aria-label="Image gallery"
    >
      {/* Scrollable container for large images */}
      <div className="w-full h-full overflow-auto flex items-center justify-center p-4">
        <img
          src={imageUrl}
          alt={altText}
          onClick={handleImageClick}
          className="max-w-full max-h-full object-contain cursor-zoom-in"
          style={{ touchAction: "manipulation" }} // Enable pinch-to-zoom on mobile
        />
      </div>

      {/* Close button */}
      <button
        onClick={onClose}
        className="absolute top-4 right-4 w-10 h-10 sm:w-8 sm:h-8 bg-black/50 hover:bg-black/70 rounded-full text-white text-2xl flex items-center justify-center transition-colors focus:outline-none focus:ring-2 focus:ring-brand-DEFAULT"
        aria-label="Close image gallery"
        style={{
          top: "env(safe-area-inset-top, 1rem)",
          right: "env(safe-area-inset-right, 1rem)",
        }}
      >
        ✕
      </button>

      {/* Optional: Hint text for mobile users */}
      <div className="absolute bottom-4 left-1/2 -translate-x-1/2 text-white/60 text-xs sm:text-sm pointer-events-none">
        Pinch to zoom • Tap to close
      </div>
    </div>
  );
}
