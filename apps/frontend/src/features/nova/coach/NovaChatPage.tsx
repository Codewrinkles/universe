import { useState } from "react";
import { useParams } from "react-router-dom";
import { Helmet } from "react-helmet-async";
import { ChatArea } from "./components/ChatArea";
import { useChat } from "./hooks/useChat";

/**
 * NovaChatPage - Main chat page for Nova
 *
 * Handles both new conversations (/nova/c/new) and existing ones (/nova/c/:id).
 * The ChatArea component handles the display logic based on message state.
 */
export function NovaChatPage(): JSX.Element {
  const { conversationId } = useParams<{ conversationId: string }>();
  const isNewChat = conversationId === "new" || !conversationId;

  const { messages, isStreaming, sendMessage } = useChat(conversationId);
  const [selectedPrompt, setSelectedPrompt] = useState("");

  const handleSelectPrompt = (prompt: string): void => {
    setSelectedPrompt(prompt);
  };

  const handleSendMessage = (content: string): void => {
    sendMessage(content);
    setSelectedPrompt(""); // Clear the selected prompt after sending
  };

  const pageTitle = isNewChat
    ? "New Chat • Nova"
    : `Chat • Nova`;

  return (
    <>
      <Helmet>
        <title>{pageTitle}</title>
        <meta name="description" content="Nova - AI Learning Coach for software development" />
      </Helmet>

      <ChatArea
        messages={messages}
        isStreaming={isStreaming}
        onSendMessage={handleSendMessage}
        selectedPrompt={selectedPrompt}
        onSelectPrompt={handleSelectPrompt}
      />
    </>
  );
}
