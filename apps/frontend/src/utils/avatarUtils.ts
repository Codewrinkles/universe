import { config } from "../config";

/**
 * Builds the full avatar URL from a potentially relative or absolute path
 *
 * @param avatarUrl - The avatar URL (can be absolute from OAuth providers or relative from uploads)
 * @returns Full avatar URL or null if no avatar
 *
 * @example
 * // OAuth avatar (absolute URL)
 * buildAvatarUrl("https://avatars.githubusercontent.com/u/123?v=4")
 * // => "https://avatars.githubusercontent.com/u/123?v=4"
 *
 * // Uploaded avatar (relative path)
 * buildAvatarUrl("/uploads/avatars/abc123.jpg")
 * // => "https://localhost:7280/uploads/avatars/abc123.jpg"
 */
export function buildAvatarUrl(avatarUrl: string | null | undefined): string | null {
  if (!avatarUrl) {
    return null;
  }

  // If already an absolute URL (OAuth providers), use as-is
  const isAbsolute = avatarUrl.startsWith('http://') || avatarUrl.startsWith('https://');
  if (isAbsolute) {
    return avatarUrl;
  }

  // For uploaded avatars (relative paths), prepend backend URL
  return `${config.api.baseUrl}${avatarUrl}`;
}
