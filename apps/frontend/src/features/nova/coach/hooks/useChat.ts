import { useState, useCallback, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import type { Message } from "../../types";
import { config } from "../../../../config";
import { apiRequest, ApiError, getAccessToken, isTokenExpired, isTokenExpiringSoon, refreshAccessToken } from "../../../../utils/api";

interface GetConversationResponse {
  id: string;
  title: string | null;
  createdAt: string;
  lastMessageAt: string;
  messages: Array<{
    id: string;
    role: "user" | "assistant" | "system";
    content: string;
    createdAt: string;
  }>;
}

interface SSEStartEvent {
  type: "start";
  sessionId: string;
  isNewSession: boolean;
}

interface SSEContentEvent {
  type: "content";
  content: string;
}

interface SSEDoneEvent {
  type: "done";
  messageId: string;
  createdAt: string;
}

interface SSEErrorEvent {
  type: "error";
  message: string;
}

type SSEEvent = SSEStartEvent | SSEContentEvent | SSEDoneEvent | SSEErrorEvent;

interface UseChatReturn {
  messages: Message[];
  isStreaming: boolean;
  error: string | null;
  sendMessage: (content: string) => void;
}

/**
 * useChat - Hook for managing chat state and sending messages with streaming
 *
 * Connects to the Nova API for real conversations with Nova.
 * Uses Server-Sent Events (SSE) for streaming responses.
 *
 * @param conversationId - Conversation ID to load existing messages, or "new" for new conversation
 */
export function useChat(conversationId?: string): UseChatReturn {
  const navigate = useNavigate();
  const [messages, setMessages] = useState<Message[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(
    conversationId && conversationId !== "new" ? conversationId : null
  );
  const abortControllerRef = useRef<AbortController | null>(null);

  // Load existing conversation messages
  useEffect(() => {
    if (!conversationId || conversationId === "new") {
      setMessages([]);
      setCurrentSessionId(null);
      return;
    }

    const loadConversation = async (): Promise<void> => {
      try {
        const response = await apiRequest<GetConversationResponse>(
          config.api.endpoints.novaSession(conversationId)
        );
        setMessages(response.messages);
        setCurrentSessionId(conversationId);
        setError(null);
      } catch (err) {
        if (err instanceof ApiError) {
          if (err.statusCode === 404) {
            setError("Conversation not found");
          } else if (err.statusCode === 403) {
            setError("You don't have access to this conversation");
          } else {
            setError(err.message);
          }
        } else {
          setError("Failed to load conversation");
        }
      }
    };

    loadConversation();
  }, [conversationId]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort();
    };
  }, []);

  const sendMessage = useCallback(async (content: string): Promise<void> => {
    // Cancel any pending stream
    abortControllerRef.current?.abort();
    abortControllerRef.current = new AbortController();

    // Optimistically add user message
    const userMessage: Message = {
      id: `temp-${Date.now()}`,
      role: "user",
      content,
      createdAt: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setIsStreaming(true);
    setError(null);

    // Track if we've added the assistant message yet
    let assistantMessageAdded = false;

    // Helper to make the streaming request with token refresh support
    const makeStreamRequest = async (isRetry = false): Promise<Response> => {
      let token = getAccessToken();

      // Proactive refresh: if token is expired or expiring soon, refresh first
      if (token && (isTokenExpired(token) || isTokenExpiringSoon(token))) {
        const refreshed = await refreshAccessToken();
        if (refreshed) {
          token = getAccessToken();
        } else if (!isRetry) {
          // Refresh failed - this will likely fail, but let's try
          // The 401 handler below will take care of it
        }
      }

      const response = await fetch(config.api.endpoints.novaChatStream, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        credentials: "include",
        body: JSON.stringify({
          message: content,
          sessionId: currentSessionId,
        }),
        signal: abortControllerRef.current?.signal,
      });

      // Handle 401: try to refresh and retry once
      if (response.status === 401 && !isRetry) {
        const refreshed = await refreshAccessToken();
        if (refreshed) {
          return makeStreamRequest(true);
        }
        // Refresh failed - dispatch event to logout
        window.dispatchEvent(new CustomEvent("auth:unauthorized"));
      }

      return response;
    };

    try {
      const response = await makeStreamRequest();

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const reader = response.body?.getReader();
      if (!reader) {
        throw new Error("No response body");
      }

      const decoder = new TextDecoder();
      let buffer = "";
      let sessionId: string | null = null;
      let isNewSession = false;
      let messageId: string | null = null;
      let createdAt: string | null = null;

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });

        // Process complete SSE messages (data: {...}\n\n)
        const lines = buffer.split("\n\n");
        buffer = lines.pop() || "";

        for (const line of lines) {
          if (line.startsWith("data: ")) {
            const jsonStr = line.slice(6);
            try {
              const event = JSON.parse(jsonStr) as SSEEvent;

              switch (event.type) {
                case "start":
                  sessionId = event.sessionId;
                  isNewSession = event.isNewSession;
                  break;

                case "content":
                  // Add assistant message on first content, then append
                  if (!assistantMessageAdded) {
                    assistantMessageAdded = true;
                    setMessages((prev) => [
                      ...prev,
                      {
                        id: `streaming-${Date.now()}`,
                        role: "assistant",
                        content: event.content,
                        createdAt: new Date().toISOString(),
                      },
                    ]);
                  } else {
                    setMessages((prev) => {
                      const updated = [...prev];
                      const lastMessage = updated[updated.length - 1];
                      if (lastMessage && lastMessage.role === "assistant") {
                        updated[updated.length - 1] = {
                          ...lastMessage,
                          content: lastMessage.content + event.content,
                        };
                      }
                      return updated;
                    });
                  }
                  break;

                case "done":
                  messageId = event.messageId;
                  createdAt = event.createdAt;
                  break;

                case "error":
                  throw new Error(event.message);
              }
            } catch (parseError) {
              if (parseError instanceof SyntaxError) {
                console.error("Failed to parse SSE event:", jsonStr);
              } else {
                throw parseError;
              }
            }
          }
        }
      }

      // Update message with final ID
      if (messageId && createdAt) {
        setMessages((prev) => {
          const updated = [...prev];
          const lastMessage = updated[updated.length - 1];
          if (lastMessage && lastMessage.role === "assistant") {
            updated[updated.length - 1] = {
              ...lastMessage,
              id: messageId,
              createdAt: createdAt,
            };
          }
          return updated;
        });
      }

      // Navigate to new session URL if this was a new conversation
      if (isNewSession && sessionId) {
        setCurrentSessionId(sessionId);
        navigate(`/nova/c/${sessionId}`, { replace: true });
      }
    } catch (err) {
      if (err instanceof Error && err.name === "AbortError") {
        // Request was cancelled, don't show error
        return;
      }

      // Remove the optimistic messages on error
      setMessages((prev) =>
        prev.filter((m) => {
          if (m.id === userMessage.id) return false;
          if (assistantMessageAdded && m.id.startsWith("streaming-")) return false;
          return true;
        })
      );

      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError("Failed to send message. Please try again.");
      }
    } finally {
      setIsStreaming(false);
    }
  }, [currentSessionId, navigate]);

  // Wrap in useCallback to maintain stable reference
  const sendMessageHandler = useCallback((content: string) => {
    sendMessage(content);
  }, [sendMessage]);

  return {
    messages,
    isStreaming,
    error,
    sendMessage: sendMessageHandler,
  };
}
