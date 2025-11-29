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

export interface Mention {
  profileId: string;
  handle: string;
}

export interface Pulse {
  id: string;
  author: PulseAuthor;
  content: string;
  type: "original" | "repulse" | "reply";
  createdAt: string;
  engagement: PulseEngagement;
  isLikedByCurrentUser: boolean;
  isFollowingAuthor: boolean;
  isBookmarkedByCurrentUser: boolean;
  parentPulseId?: string | null;
  imageUrl?: string | null;
  linkPreview?: PulseLinkPreview;
  repulsedPulse?: RepulsedPulse;
  mentions: Mention[];
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

export interface Hashtag {
  id: string;
  tag: string;
  tagDisplay: string;
  pulseCount: number;
  lastUsedAt: string;
}

export interface HashtagsResponse {
  hashtags: Hashtag[];
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
  role: string;
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
  location: string | null;
  websiteUrl: string | null;
  role: string;
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
  location: string | null;
  websiteUrl: string | null;
  role: string;
}

// Profile API types
export interface UpdateProfileRequest {
  name: string;
  bio: string | null;
  handle: string | null;
  location: string | null;
  websiteUrl: string | null;
}

export interface UpdateProfileResponse {
  profileId: string;
  name: string;
  handle: string | null;
  bio: string | null;
  avatarUrl: string | null;
  location: string | null;
  websiteUrl: string | null;
}

// Onboarding API types
export interface OnboardingStatus {
  isCompleted: boolean;
  hasHandle: boolean;
  hasBio: boolean;
  hasAvatar: boolean;
  hasPostedPulse: boolean;
  followingCount: number;
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

// ============================================
// Social/Follow Types (matching backend DTOs)
// ============================================

export interface FollowResult {
  success: boolean;
}

export interface FollowerDto {
  profileId: string;
  name: string;
  handle: string;
  avatarUrl: string | null;
  bio: string | null;
  followedAt: string;
}

export interface FollowingDto {
  profileId: string;
  name: string;
  handle: string;
  avatarUrl: string | null;
  bio: string | null;
  followedAt: string;
}

export interface ProfileSuggestion {
  profileId: string;
  name: string;
  handle: string;
  avatarUrl: string | null;
  bio: string | null;
  mutualFollowCount: number;
}

export interface FollowersResponse {
  followers: FollowerDto[];
  totalCount: number;
  nextCursor: string | null;
  hasMore: boolean;
}

export interface FollowingResponse {
  following: FollowingDto[];
  totalCount: number;
  nextCursor: string | null;
  hasMore: boolean;
}

export interface SuggestedProfilesResponse {
  suggestions: ProfileSuggestion[];
}

export interface IsFollowingResponse {
  isFollowing: boolean;
}

export interface HandleSearchResult {
  profileId: string;
  handle: string;
  name: string;
  avatarUrl: string | null;
}

export interface SearchHandlesResponse {
  handles: HandleSearchResult[];
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
