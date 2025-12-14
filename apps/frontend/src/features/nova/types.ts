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
