import { useState, useEffect, useCallback } from "react";
import type { Conversation, ConversationGroup } from "../../types";
import { config } from "../../../../config";
import { apiRequest, ApiError } from "../../../../utils/api";

interface GetConversationsResponse {
  sessions: Array<{
    id: string;
    title: string | null;
    createdAt: string;
    lastMessageAt: string;
    messageCount: number;
  }>;
  hasMore: boolean;
}

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
  error: string | null;
  refetch: () => void;
  deleteConversation: (id: string) => Promise<void>;
}

/**
 * useConversations - Hook for fetching and managing conversation list
 *
 * Connects to the Nova API to fetch real conversation history.
 */
export function useConversations(): UseConversationsReturn {
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchConversations = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await apiRequest<GetConversationsResponse>(
        config.api.endpoints.novaSessions
      );

      const mapped: Conversation[] = response.sessions.map((c) => ({
        id: c.id,
        title: c.title || "Untitled conversation",
        createdAt: c.createdAt,
        lastMessageAt: c.lastMessageAt,
        messageCount: c.messageCount,
      }));

      setConversations(mapped);
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.statusCode === 401) {
          // User not authenticated - this is expected for new users
          setConversations([]);
        } else {
          setError(err.message);
        }
      } else {
        setError("Failed to load conversations");
      }
    } finally {
      setIsLoading(false);
    }
  }, []);

  const deleteConversation = useCallback(async (id: string): Promise<void> => {
    try {
      await apiRequest(config.api.endpoints.novaSession(id), {
        method: "DELETE",
      });

      // Remove from local state
      setConversations((prev) => prev.filter((c) => c.id !== id));
    } catch (err) {
      if (err instanceof ApiError) {
        throw err;
      }
      throw new Error("Failed to delete conversation");
    }
  }, []);

  useEffect(() => {
    fetchConversations();
  }, [fetchConversations]);

  return {
    conversations,
    groupedConversations: groupConversationsByDate(conversations),
    isLoading,
    error,
    refetch: fetchConversations,
    deleteConversation,
  };
}
