import { useState } from "react";
import type { Conversation, ConversationGroup } from "../../types";

// Mock data for development
const MOCK_CONVERSATIONS: Conversation[] = [
  {
    id: "conv-1",
    title: "Clean Architecture in .NET",
    createdAt: new Date().toISOString(),
    lastMessageAt: new Date().toISOString(),
    messageCount: 8,
    topicEmoji: "🏗️",
  },
  {
    id: "conv-2",
    title: "CQRS vs traditional MVC",
    createdAt: new Date().toISOString(),
    lastMessageAt: new Date().toISOString(),
    messageCount: 5,
    topicEmoji: "📦",
  },
  {
    id: "conv-3",
    title: "DDD aggregate design",
    createdAt: new Date(Date.now() - 86400000).toISOString(),
    lastMessageAt: new Date(Date.now() - 86400000).toISOString(),
    messageCount: 12,
    topicEmoji: "🎯",
  },
  {
    id: "conv-4",
    title: "Microservices communication",
    createdAt: new Date(Date.now() - 259200000).toISOString(),
    lastMessageAt: new Date(Date.now() - 259200000).toISOString(),
    messageCount: 15,
    topicEmoji: "🔗",
  },
];

function groupConversationsByDate(conversations: Conversation[]): ConversationGroup[] {
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const yesterday = new Date(today.getTime() - 86400000);
  const thisWeek = new Date(today.getTime() - 7 * 86400000);

  const groups: ConversationGroup[] = [];
  const todayConvos: Conversation[] = [];
  const yesterdayConvos: Conversation[] = [];
  const thisWeekConvos: Conversation[] = [];
  const olderConvos: Conversation[] = [];

  for (const convo of conversations) {
    const convoDate = new Date(convo.lastMessageAt);
    if (convoDate >= today) {
      todayConvos.push(convo);
    } else if (convoDate >= yesterday) {
      yesterdayConvos.push(convo);
    } else if (convoDate >= thisWeek) {
      thisWeekConvos.push(convo);
    } else {
      olderConvos.push(convo);
    }
  }

  if (todayConvos.length > 0) groups.push({ label: "Today", conversations: todayConvos });
  if (yesterdayConvos.length > 0) groups.push({ label: "Yesterday", conversations: yesterdayConvos });
  if (thisWeekConvos.length > 0) groups.push({ label: "This Week", conversations: thisWeekConvos });
  if (olderConvos.length > 0) groups.push({ label: "Older", conversations: olderConvos });

  return groups;
}

interface UseConversationsReturn {
  conversations: Conversation[];
  groupedConversations: ConversationGroup[];
  isLoading: boolean;
}

/**
 * useConversations - Hook for fetching and managing conversation list
 *
 * For MVP, this returns mock data. Will be replaced with real API calls.
 */
export function useConversations(): UseConversationsReturn {
  const [isLoading] = useState(false);

  return {
    conversations: MOCK_CONVERSATIONS,
    groupedConversations: groupConversationsByDate(MOCK_CONVERSATIONS),
    isLoading,
  };
}
