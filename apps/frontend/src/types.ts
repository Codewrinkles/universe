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

// ============================================
// Pulse Types (matching backend DTOs)
// ============================================

export interface PulseAuthor {
  id: string;
  name: string;
  handle: string;
  avatarUrl: string | null;
}

export interface PulseEngagement {
  replyCount: number;
  repulseCount: number;
  likeCount: number;
  viewCount: number;
}

export interface PulseImage {
  url: string;
  altText?: string;
}

export interface PulseLinkPreview {
  url: string;
  title: string;
  description?: string;
  imageUrl?: string;
  domain: string;
}

export interface RepulsedPulse {
  id: string;
  author: PulseAuthor;
  content: string;
  createdAt: string;
  isDeleted: boolean;
  image?: PulseImage;
  linkPreview?: PulseLinkPreview;
}

export interface Pulse {
  id: string;
  author: PulseAuthor;
  content: string;
  type: "original" | "repulse" | "reply";
  createdAt: string;
  engagement: PulseEngagement;
  isLikedByCurrentUser: boolean;
  parentPulseId?: string | null;
  imageUrl?: string | null;
  linkPreview?: PulseLinkPreview;
  repulsedPulse?: RepulsedPulse;
}

export interface FeedResponse {
  pulses: Pulse[];
  nextCursor: string | null;
  hasMore: boolean;
}

export interface ThreadResponse {
  parentPulse: Pulse;
  replies: Pulse[];
  totalReplyCount: number;
  nextCursor: string | null;
  hasMore: boolean;
}

export interface CreatePulseRequest {
  content: string;
}

export interface CreatePulseResponse {
  pulseId: string;
  content: string;
  createdAt: string;
  imageUrl?: string | null;
}

// Legacy Post types (deprecated - use Pulse types above)
export type PostAuthor = PulseAuthor;
export type Post = Pulse;
export type RepostedPost = RepulsedPulse;

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
