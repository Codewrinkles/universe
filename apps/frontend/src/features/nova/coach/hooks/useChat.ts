import { useState, useCallback } from "react";
import type { Message } from "../../types";

// Mock responses for development
const MOCK_RESPONSES = {
  default: `Great question! Let me help you understand this concept.

When approaching software architecture, it's important to consider:

1. **Separation of concerns** - Each component should have a single responsibility
2. **Dependency management** - Dependencies should point inward toward the domain
3. **Testability** - Your design should make testing straightforward

Would you like me to dive deeper into any of these areas?`,

  architecture: `Clean Architecture is a software design philosophy that separates the elements of a design into ring levels.

**The key layers are:**

• **Domain** (innermost) - Business entities and rules
• **Application** - Use cases and orchestration
• **Infrastructure** - External concerns (DB, APIs, etc.)
• **Presentation** - UI and API endpoints

The critical rule: dependencies can only point inward. The domain layer knows nothing about the outer layers.

This gives you:
- Testable business logic
- Swappable infrastructure
- Framework independence

Want me to show you how this looks in a .NET project?`,

  roadmap: `I'd love to help you create a learning roadmap! Here's a suggested path to become a better software architect:

**Phase 1: Foundations (Current)**
- [ ] SOLID principles deep dive
- [ ] Design patterns (Gang of Four)
- [ ] Clean Code practices

**Phase 2: Architecture Patterns**
- [ ] Clean Architecture
- [ ] Domain-Driven Design basics
- [ ] CQRS and Event Sourcing

**Phase 3: System Design**
- [ ] Distributed systems concepts
- [ ] Microservices vs Monoliths
- [ ] API design best practices

**Phase 4: Advanced Topics**
- [ ] Event-driven architecture
- [ ] Resilience patterns
- [ ] Performance optimization

Should I start with any of these topics?`,
};

function getMockResponse(userMessage: string): string {
  const lowerMessage = userMessage.toLowerCase();

  if (lowerMessage.includes("clean architecture") || lowerMessage.includes("architecture")) {
    return MOCK_RESPONSES["architecture"];
  }
  if (lowerMessage.includes("roadmap") || lowerMessage.includes("learning path") || lowerMessage.includes("become")) {
    return MOCK_RESPONSES["roadmap"];
  }

  return MOCK_RESPONSES["default"];
}

interface UseChatReturn {
  messages: Message[];
  isStreaming: boolean;
  sendMessage: (content: string) => void;
}

/**
 * useChat - Hook for managing chat state and sending messages
 *
 * For MVP, this uses mock responses. Will be replaced with real API calls.
 *
 * @param _conversationId - Conversation ID (unused in MVP, will load history in future)
 */
export function useChat(_conversationId?: string): UseChatReturn {
  const [messages, setMessages] = useState<Message[]>([]);
  const [isStreaming, setIsStreaming] = useState(false);

  const sendMessage = useCallback((content: string) => {
    // Add user message
    const userMessage: Message = {
      id: `msg-${Date.now()}`,
      role: "user",
      content,
      createdAt: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setIsStreaming(true);

    // Simulate API delay and response
    setTimeout(() => {
      const assistantMessage: Message = {
        id: `msg-${Date.now() + 1}`,
        role: "assistant",
        content: getMockResponse(content),
        createdAt: new Date().toISOString(),
      };

      setMessages((prev) => [...prev, assistantMessage]);
      setIsStreaming(false);
    }, 1500);
  }, []);

  return {
    messages,
    isStreaming,
    sendMessage,
  };
}
