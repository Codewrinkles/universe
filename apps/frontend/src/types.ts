/**
 * Shared type definitions for the Codewrinkles application
 */

export type Theme = "dark" | "light";

export type AuthMode = "password" | "magic";

export interface App {
  id: string;
  name: string;
  accent: "sky" | "violet" | "brand" | "amber" | "emerald";
  description: string;
}

// Post author info
export interface PostAuthor {
  id: string;
  name: string;
  handle: string;
  avatarUrl?: string;
}

// Media attachments
export interface PostImage {
  url: string;
  alt?: string;
  width?: number;
  height?: number;
}

export interface PostLinkPreview {
  url: string;
  title: string;
  description?: string;
  imageUrl?: string;
  domain: string;
}

// Reposted content (re-pulse)
export interface RepostedPost {
  id: number;
  author: PostAuthor;
  content: string;
  timeAgo: string;
  images?: PostImage[];
  linkPreview?: PostLinkPreview;
}

// Main Post interface
export interface Post {
  id: number;
  author: PostAuthor;
  content: string;
  timeAgo: string;

  // Engagement stats
  replyCount?: number;
  repostCount?: number;
  likeCount?: number;
  viewCount?: number;

  // Media attachments (optional)
  images?: PostImage[];
  linkPreview?: PostLinkPreview;

  // Repost (quote tweet style)
  repostedPost?: RepostedPost;

  // Thread indicator
  isThread?: boolean;
  threadLength?: number;
}

export interface Module {
  id: number;
  title: string;
  duration: string;
  status: "done" | "in-progress" | "todo";
}

export interface ChatMessage {
  id: number;
  from: "system" | "twin" | "you";
  text: string;
}

export interface SettingsSection {
  id: "profile" | "account" | "apps" | "notifications";
  label: string;
}

export interface OnboardingStep {
  id: number;
  title: string;
  description: string;
  chips: string[];
}

// ============================================
// Auth Types
// ============================================

export interface RegisterRequest {
  email: string;
  password: string;
  name: string;
  handle?: string;
}

export interface RegisterResponse {
  identityId: string;
  profileId: string;
  email: string;
  name: string;
  handle: string | null;
  accessToken: string;
  refreshToken: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  identityId: string;
  profileId: string;
  email: string;
  name: string;
  handle: string | null;
  bio: string | null;
  avatarUrl: string | null;
  accessToken: string;
  refreshToken: string;
}

export interface User {
  identityId: string;
  profileId: string;
  email: string;
  name: string;
  handle: string | null;
  bio: string | null;
  avatarUrl: string | null;
}

// Profile API types
export interface UpdateProfileRequest {
  name: string;
  bio: string | null;
  handle: string | null;
}

export interface UpdateProfileResponse {
  profileId: string;
  name: string;
  handle: string | null;
  bio: string | null;
  avatarUrl: string | null;
}

export interface UploadAvatarResponse {
  profileId: string;
  avatarUrl: string;
}

// Change Password API types
export interface ChangePasswordRequest {
  identityId: string;
  currentPassword: string;
  newPassword: string;
}

export interface ChangePasswordResponse {
  success: boolean;
}

export interface ApiErrorResponse {
  error?: string;
  title?: string;
  detail?: string;
  status?: number;
  // ASP.NET Core ProblemDetails validation errors format: { "FieldName": ["error1", "error2"] }
  errors?: Record<string, string[]>;
}

export interface ValidationErrorItem {
  property: string;
  message: string;
}

export interface FormErrors {
  email?: string;
  password?: string;
  name?: string;
  handle?: string;
  general?: string;
}
