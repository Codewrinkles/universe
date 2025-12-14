import { useEffect, useRef } from "react";
import type { Message } from "../../types";
import { CodyMessage } from "./CodyMessage";
import { UserMessage } from "./UserMessage";
import { StreamingIndicator } from "./StreamingIndicator";

interface MessageListProps {
  messages: Message[];
  isStreaming?: boolean;
}

/**
 * MessageList - Scrollable container for chat messages
 *
 * Features:
 * - Auto-scrolls to bottom on new messages
 * - Renders different bubble types based on role
 * - Shows streaming indicator when Cody is responding
 */
export function MessageList({ messages, isStreaming = false }: MessageListProps): JSX.Element {
  const bottomRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to bottom when messages change
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, isStreaming]);

  return (
    <div className="flex-1 overflow-y-auto">
      <div className="py-4">
        {messages.map((message) => {
          if (message.role === "assistant") {
            return <CodyMessage key={message.id} message={message} />;
          }
          if (message.role === "user") {
            return <UserMessage key={message.id} message={message} />;
          }
          // System messages could be rendered differently if needed
          return null;
        })}

        {isStreaming && <StreamingIndicator />}

        {/* Scroll anchor */}
        <div ref={bottomRef} />
      </div>
    </div>
  );
}
