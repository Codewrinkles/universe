/**
 * Shared types for Nova - AI Learning Coach
 */

export interface Conversation {
  id: string;
  title: string;
  createdAt: string;
  lastMessageAt: string;
  messageCount: number;
  /** Auto-detected topic emoji based on conversation content */
  topicEmoji?: string;
}

export interface Message {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
  createdAt: string;
}

/** Grouped conversations by time period */
export interface ConversationGroup {
  label: string;
  conversations: Conversation[];
}

/** Learning style preferences */
export type LearningStyle = "ExamplesFirst" | "TheoryFirst" | "HandsOn";

/** Preferred explanation depth */
export type PreferredPace = "QuickOverview" | "Balanced" | "DeepDive";

/** Nova-specific learner profile */
export interface LearnerProfile {
  id: string;
  profileId: string;
  currentRole: string | null;
  experienceYears: number | null;
  primaryTechStack: string | null;
  currentProject: string | null;
  learningGoals: string | null;
  learningStyle: LearningStyle | null;
  preferredPace: PreferredPace | null;
  identifiedStrengths: string | null;
  identifiedStruggles: string | null;
  hasUserData: boolean;
  createdAt: string;
  updatedAt: string;
}
