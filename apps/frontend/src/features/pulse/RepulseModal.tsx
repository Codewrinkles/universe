import { useState, useRef } from "react";
import type { Post } from "../../types";
import { useAuth } from "../../hooks/useAuth";
import { useCreateRepulse } from "./hooks/useCreateRepulse";
import { config } from "../../config";
import { formatTimeAgo } from "../../utils/timeUtils";

export interface RepulseModalProps {
  post: Post;
  onClose: () => void;
  onSuccess?: () => void;
}

export function RepulseModal({ post, onClose, onSuccess }: RepulseModalProps): JSX.Element {
  const { user } = useAuth();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // State
  const [value, setValue] = useState("");
  const [selectedImage, setSelectedImage] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);

  // Hooks
  const { createRepulse, isCreating, error } = useCreateRepulse();

  const maxChars = 300;
  const charsLeft = maxChars - value.length;
  const isOverLimit = charsLeft < 0;

  const handleSubmit = async (): Promise<void> => {
    if (value.trim().length === 0 || isOverLimit || isCreating) {
      return;
    }

    try {
      await createRepulse(post.id, value, selectedImage);

      // Clear form on success
      setValue("");
      setSelectedImage(null);
      setImagePreview(null);
      onSuccess?.();
      onClose();
    } catch (err) {
      // Error is handled by the hook
      console.error("Failed to create repulse:", err);
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

  const handleBackdropClick = (e: React.MouseEvent<HTMLDivElement>): void => {
    if (e.target === e.currentTarget) {
      onClose();
    }
  };

  const avatarUrl = user?.avatarUrl
    ? `${config.api.baseUrl}${user.avatarUrl}`
    : null;

  const postAvatarUrl = post.author.avatarUrl
    ? (post.author.avatarUrl.startsWith('http') ? post.author.avatarUrl : `${config.api.baseUrl}${post.author.avatarUrl}`)
    : null;

  const timeAgo = formatTimeAgo(post.createdAt);

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center bg-black/70 pt-20"
      onClick={handleBackdropClick}
    >
      <div className="w-full max-w-[600px] bg-surface-card1 rounded-2xl border border-border mx-4 max-h-[80vh] overflow-y-auto custom-scrollbar">
        {/* Header */}
        <div className="sticky top-0 z-10 flex items-center justify-between border-b border-border bg-surface-card1/90 backdrop-blur px-4 py-3">
          <button
            type="button"
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-full hover:bg-surface-card2 transition-colors text-text-primary"
            title="Close"
          >
            ✕
          </button>
          <h2 className="text-base font-semibold text-text-primary">Quote Pulse</h2>
          <div className="w-8" /> {/* Spacer for symmetry */}
        </div>

        {/* Body */}
        <div className="p-4 space-y-4">
          {/* Original pulse being repulsed */}
          <div className="rounded-2xl border border-border p-3 bg-surface-card2/30">
            <div className="flex gap-2">
              {postAvatarUrl ? (
                <img
                  src={postAvatarUrl}
                  alt={post.author.name}
                  className="h-8 w-8 rounded-full object-cover flex-shrink-0"
                />
              ) : (
                <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-surface-card2 border border-border text-xs font-semibold text-text-primary">
                  {post.author.name.charAt(0).toUpperCase()}
                </div>
              )}
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-1 text-sm">
                  <span className="font-medium text-text-primary truncate">{post.author.name}</span>
                  <span className="text-text-tertiary truncate">@{post.author.handle}</span>
                  <span className="text-text-tertiary">·</span>
                  <span className="text-text-tertiary">{timeAgo}</span>
                </div>
                <p className="mt-1 text-sm text-text-primary line-clamp-5">{post.content}</p>
                {post.imageUrl && (
                  <div className="mt-2 rounded-xl overflow-hidden border border-border">
                    <img
                      src={`${config.api.baseUrl}${post.imageUrl}`}
                      alt="Original pulse attachment"
                      className="w-full object-contain max-h-48"
                    />
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Composer */}
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
              <textarea
                ref={textareaRef}
                value={value}
                onChange={(e) => setValue(e.target.value)}
                rows={4}
                placeholder="Add your thoughts..."
                className="w-full resize-none bg-transparent text-lg text-text-primary placeholder:text-text-tertiary focus:outline-none custom-scrollbar"
                autoFocus
              />
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
              {error && (
                <div className="text-sm text-red-400">{error}</div>
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
                    disabled={value.trim().length === 0 || isOverLimit || isCreating}
                    onClick={handleSubmit}
                    className={`rounded-full px-4 py-1.5 text-sm font-semibold transition-colors ${
                      value.trim().length === 0 || isOverLimit || isCreating
                        ? "bg-brand-soft/50 text-black/50 cursor-not-allowed"
                        : "bg-brand-soft text-black hover:bg-brand"
                    }`}
                  >
                    {isCreating ? "Repulsing..." : "Repulse"}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
