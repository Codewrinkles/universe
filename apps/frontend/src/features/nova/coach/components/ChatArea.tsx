import type { Message } from "../../types";
import { MessageList } from "./MessageList";
import { ChatInput } from "./ChatInput";
import { EmptyState } from "./EmptyState";
import { StarterCards } from "./StarterCards";

interface ChatAreaProps {
  messages: Message[];
  isStreaming: boolean;
  error: string | null;
  onSendMessage: (content: string) => void;
  selectedPrompt: string;
  onSelectPrompt: (prompt: string) => void;
}

/**
 * ChatArea - Main chat container
 *
 * Handles the display logic:
 * - Shows empty state + starter cards when no messages
 * - Shows message list when conversation has started
 * - Always shows input at bottom
 */
export function ChatArea({
  messages,
  isStreaming,
  error,
  onSendMessage,
  selectedPrompt,
  onSelectPrompt,
}: ChatAreaProps): JSX.Element {
  const hasMessages = messages.length > 0;

  return (
    <div className="flex flex-col h-full">
      {hasMessages ? (
        // Conversation view
        <MessageList messages={messages} isStreaming={isStreaming} />
      ) : (
        // Empty state view - centered vertically
        <div className="flex-1 flex flex-col justify-center py-4">
          <EmptyState />
          <div className="mt-6">
            <StarterCards onSelectPrompt={onSelectPrompt} />
          </div>
        </div>
      )}

      {/* Error message */}
      {error && (
        <div className="px-4 py-2 mx-4 mb-2 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 text-sm">
          {error}
        </div>
      )}

      {/* Input always at bottom */}
      <div className="flex-shrink-0 bg-surface-page">
        <ChatInput
          onSend={onSendMessage}
          disabled={isStreaming}
          initialValue={selectedPrompt}
        />
      </div>
    </div>
  );
}
