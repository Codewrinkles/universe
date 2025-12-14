# Nova UI/UX Design Proposal

> **Based on**: Deep research into modern AI chat interfaces, learning platforms, and the Codewrinkles vision
> **Status**: MVP Implemented (UI only, no backend)
> **Date**: 2024-12-14
> **Last Updated**: 2024-12-14

---

## Executive Summary

Nova is not just another AI chatbot. It's **the go-to AI coach for technical learning** - a specialized, authoritative companion that helps developers grow. The UI/UX must reflect this positioning: professional yet approachable, focused yet powerful, educational yet engaging.

**Key differentiators from generic chat UIs:**
1. **Learning-first** - Progress tracking, roadmaps, knowledge gaps
2. **Coach personality** - Named assistant with consistent voice
3. **Specialized scope** - Technical learning, not general-purpose
4. **Clean, focused UI** - No clutter, conversation is king

---

## Meet Cody - The Nova Coach

### Why a Named Coach?

Research shows that **AI tools with personality build better connections**. A named coach:
- Creates emotional attachment and increases retention
- Makes the experience feel like mentorship, not a tool
- Allows for consistent voice and tone
- Differentiates from generic "AI Assistant" competitors

### Cody's Persona

| Attribute | Description |
|-----------|-------------|
| **Name** | Cody (code + buddy, technical + friendly) |
| **Role** | Senior developer who loves teaching |
| **Tone** | Encouraging but honest, technical but accessible |
| **Voice** | "Let me explain...", "Great question!", "Here's what I'd recommend..." |
| **Avatar** | Stylized icon with violet accent (Nova's color) |

**Cody's personality traits:**
- Acknowledges when something is complex
- Admits knowledge limitations honestly
- Celebrates user progress
- Suggests next steps proactively
- References sources transparently

---

## Layout Architecture

### App-Specific Layouts Philosophy

Each Codewrinkles app has its own layout optimized for its purpose:

| App | Layout | Purpose |
|-----|--------|---------|
| **Pulse** | 3-column Twitter-style | Social feed browsing, quick scanning |
| **Admin** | Sidebar + full-width content | Dashboard metrics, data management |
| **Nova** | 2-panel chat-focused | Deep learning conversations |

```
PULSE (Social)                    ADMIN (Dashboard)                NOVA (Learning)
┌─────┬───────┬─────┐            ┌────┬──────────────┐            ┌─────┬───────────┐
│ Nav │ Feed  │ R   │            │Nav │   Content    │            │Side │   Chat    │
│240px│ 600px │288px│            │192 │  (flex-1)    │            │280px│  (flex-1) │
│     │       │     │            │    │              │            │     │           │
│     │       │     │            │    │  ┌────┐      │            │Convs│  Messages │
│     │       │     │            │    │  │Card│      │            │     │           │
│     │       │     │            │    │  └────┘      │            │Paths│  Input    │
└─────┴───────┴─────┘            └────┴──────────────┘            └─────┴───────────┘
```

### Nova Two-Panel Layout (Desktop)

Unlike Pulse's three-column Twitter-style layout, Nova uses a **two-panel layout with router outlet** optimized for learning conversations.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            HEADER                                        │
├────────────────────┬────────────────────────────────────────────────────┤
│                    │                                                     │
│   NOVA SIDEBAR     │              <Outlet />                            │
│   (280px)          │              (content changes based on route)      │
│   (shared)         │                                                     │
│                    │  ┌─────────────────────────────────────────────┐  │
│  ┌──────────────┐  │  │                                             │  │
│  │ + New Chat   │  │  │   /nova        → NovaHomePage               │  │
│  └──────────────┘  │  │   /nova/c/new  → NovaChatPage (new)         │  │
│                    │  │   /nova/c/:id  → NovaChatPage (existing)    │  │
│  CONVERSATIONS     │  │   /nova/paths  → NovaPathsPage              │  │
│  ───────────────   │  │   /nova/paths/:id → NovaPathDetailPage      │  │
│  Today             │  │                                             │  │
│  • Clean Arch...   │  │                                             │  │
│  • CQRS pattern    │  │                                             │  │
│                    │  │                                             │  │
│  Yesterday         │  └─────────────────────────────────────────────┘  │
│  • DDD entities    │                                                     │
│                    │                                                     │
│  ───────────────   │                                                     │
│  LEARNING PATHS    │                                                     │
│  ┌──────────────┐  │                                                     │
│  │ Clean Arch   │  │                                                     │
│  │ ████░░░ 60%  │  │                                                     │
│  │ [Continue →] │  │                                                     │
│  └──────────────┘  │                                                     │
│  [View all paths]──┼──→ navigates to /nova/paths                        │
│                    │                                                     │
└────────────────────┴────────────────────────────────────────────────────┘
```

**Key architectural decision:** The sidebar is part of `NovaLayout` and stays constant across all Nova routes. Only the main content area (Outlet) changes based on the current route.

### Nova Mobile Layout

On mobile, the sidebar becomes a slide-out drawer:

```
┌─────────────────────────────────┐
│  ☰  Nova           [New Chat]   │
├─────────────────────────────────┤
│                                 │
│      CONVERSATION AREA          │
│      (full width)               │
│                                 │
├─────────────────────────────────┤
│  [Ask Cody anything...]  [↑]    │
└─────────────────────────────────┘
```

---

## Component Design

### 1. Empty State / Welcome Screen

**Critical for onboarding** - Research shows users often don't know what AI can do. The empty state must showcase capabilities.

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│                    [Cody Avatar - Violet]                       │
│                                                                 │
│              "Hey! I'm Cody, your learning coach."              │
│                                                                 │
│         Ask me anything about software development,             │
│            architecture, or technical concepts.                 │
│                                                                 │
│  ┌───────────────────────┐  ┌───────────────────────┐         │
│  │ 🏗️ Architecture        │  │ 📚 Learning Paths      │         │
│  │ "Explain Clean        │  │ "Create a roadmap to  │         │
│  │ Architecture"         │  │ learn system design"  │         │
│  └───────────────────────┘  └───────────────────────┘         │
│                                                                 │
│  ┌───────────────────────┐  ┌───────────────────────┐         │
│  │ 🔧 Code Review         │  │ 💡 Best Practices      │         │
│  │ "Review this CQRS     │  │ "When should I use    │         │
│  │ implementation"       │  │ microservices?"       │         │
│  └───────────────────────┘  └───────────────────────┘         │
│                                                                 │
│         [Ask Cody anything...]                      [Send]      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Design notes:**
- 4 starter cards showing different capabilities (like Google Gemini)
- Cards are clickable and populate the input
- Warm, inviting tone from Cody
- Input always visible at bottom

### 2. Cody's Message Bubble

```
┌─────────────────────────────────────────────────────────────────┐
│  [🤖]  CODY                                                     │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  Clean Architecture separates your application into layers      │
│  with clear dependency rules. The key principle is that         │
│  dependencies should point inward.                              │
│                                                                 │
│  Here's the typical structure:                                  │
│                                                                 │
│  ```                                                            │
│  Domain (innermost) → Application → Infrastructure → API        │
│  ```                                                            │
│                                                                 │
│  The Domain layer has no dependencies on other layers.          │
│  This makes your business logic testable and portable.          │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  [📋 Copy]                                          2 min ago   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Key elements:**
- **Cody avatar + name** - Consistent identity
- **Code blocks** - Syntax highlighted, copyable
- **Copy action** - Quick utility
- **Timestamp** - Relative time
- **Clean layout** - No clutter, focus on content

### 3. User Message Bubble

```
┌─────────────────────────────────────────────────────────────────┐
│                                                           YOU   │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  How do I implement the repository pattern in .NET?             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Design notes:**
- Right-aligned, distinct background
- Simpler than Cody's messages
- Optional: Edit button to refine question

### 4. Typing/Streaming Indicator

```
┌─────────────────────────────────────────────────────────────────┐
│  [🤖]  CODY                                                     │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  The repository pattern provides an abstraction layer...        │
│  █                                                              │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│  ⏳ Cody is thinking...                                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Streaming UX:**
- Text appears word-by-word (ChatGPT style)
- Cursor blinks at end
- "Cody is thinking..." shown during retrieval phase
- Input disabled during generation

### 5. Sidebar - Conversation History

```
┌────────────────────────────┐
│  [+ New Chat]              │
│                            │
│  ─────────────────────────│
│  CONVERSATIONS             │
│  ─────────────────────────│
│                            │
│  Today                     │
│  ┌────────────────────┐   │
│  │ 🏗️ Clean Archit... │   │  ← Active (highlighted)
│  │ 3 messages         │   │
│  └────────────────────┘   │
│  ┌────────────────────┐   │
│  │ 📦 CQRS vs MVC     │   │
│  │ 8 messages         │   │
│  └────────────────────┘   │
│                            │
│  Yesterday                 │
│  ┌────────────────────┐   │
│  │ 🔧 DDD entities    │   │
│  │ 12 messages        │   │
│  └────────────────────┘   │
│                            │
│  This Week                 │
│  ┌────────────────────┐   │
│  │ 💡 Microservices   │   │
│  │ 5 messages         │   │
│  └────────────────────┘   │
│                            │
│  [Show all conversations]  │
│                            │
└────────────────────────────┘
```

**Features:**
- Grouped by time (Today, Yesterday, This Week, Older)
- Auto-generated titles from first message
- Message count indicator
- Topic emoji based on detected subject
- Hover reveals delete/rename actions
- Search conversations (future)

### 6. Sidebar - Learning Paths (Differentiator!)

```
┌────────────────────────────┐
│  ─────────────────────────│
│  YOUR LEARNING PATHS       │
│  ─────────────────────────│
│                            │
│  ┌────────────────────┐   │
│  │ Clean Architecture  │   │
│  │ ████████░░░░ 65%    │   │
│  │ 4 of 6 topics       │   │
│  │ [Continue →]        │   │
│  └────────────────────┘   │
│                            │
│  ┌────────────────────┐   │
│  │ System Design       │   │
│  │ ██░░░░░░░░░░ 15%    │   │
│  │ 2 of 12 topics      │   │
│  │ [Continue →]        │   │
│  └────────────────────┘   │
│                            │
│  [+ Create new path]       │
│                            │
└────────────────────────────┘
```

**Gamification elements:**
- Visual progress bars (Duolingo-inspired)
- Topic completion count
- "Continue" CTA to resume learning
- Creates stickiness and return visits

### 7. Input Area

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Ask Cody anything about software development...          │   │
│  │                                                          │   │
│  │                                                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [📎 Attach]  [🎯 Focus: Architecture]              [Send ↑]   │
│                                                                 │
│  💡 Tip: Be specific! "How do I..." works better than "Tell me" │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Features:**
- Auto-expanding textarea
- Attach code snippets (future)
- Focus/topic filter (future) - narrow to specific domain
- Contextual tips that rotate
- Keyboard shortcut: Cmd/Ctrl + Enter to send

### 8. Follow-up Suggestions

After Cody responds, suggest related questions:

```
┌─────────────────────────────────────────────────────────────────┐
│  💭 Related questions:                                          │
│                                                                 │
│  [How do I test Clean Architecture?]                            │
│  [Show me a .NET example]                                       │
│  [What about the Infrastructure layer?]                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Purpose:**
- Reduces articulation barrier
- Guides deeper learning
- Shows AI understanding of context
- Encourages continued engagement

---

## Color Scheme (Using Existing Tokens)

Nova uses the **violet accent** to differentiate from Pulse (sky/blue):

| Element | Color | Token |
|---------|-------|-------|
| Nova accent | Violet | `violet-400`, `violet-500` |
| Cody avatar bg | Violet soft | `violet-500/20` |
| Active conversation | Violet border | `border-violet-500/60` |
| Progress bars | Violet gradient | `from-violet-500 to-violet-400` |
| Background | Surface tokens | `bg-surface-page`, `bg-surface-card1` |
| Text | Text tokens | `text-text-primary`, `text-text-secondary` |
| Borders | Border tokens | `border-border`, `border-border-deep` |

---

## User Flows

### Flow 1: New User First Visit

```
1. User lands on /nova
2. See empty state with Cody introduction
3. 4 starter cards show capabilities
4. User clicks "Explain Clean Architecture"
5. Input populates, user can modify or send
6. Cody responds with streaming text
7. Follow-up suggestions shown
8. Sidebar shows new conversation created
```

### Flow 2: Returning User

```
1. User lands on /nova
2. Last conversation auto-loads OR empty state
3. Sidebar shows conversation history
4. User can continue or start new chat
5. Learning paths show progress (if any)
```

### Flow 3: Creating Learning Path (Future - M8)

```
1. User asks: "Create a learning path for system design"
2. Cody generates structured roadmap
3. User confirms or modifies
4. Path appears in sidebar with 0% progress
5. Each topic links to guided conversations
6. Progress updates as topics are covered
```

---

## Responsive Behavior

### Desktop (lg+)
- Two-panel layout: Sidebar (280px) + Main chat
- Sidebar always visible
- Full feature set

### Tablet (md)
- Sidebar as collapsible drawer
- Main chat full width
- Hamburger menu to toggle sidebar

### Mobile (sm)
- Sidebar hidden (slide-out drawer)
- Chat optimized for vertical scrolling
- Floating action button for new chat
- Simplified source display

---

## Accessibility Considerations

1. **Keyboard navigation** - Tab through messages, Enter to send
2. **Screen reader** - Proper ARIA labels for messages, roles
3. **Focus management** - Focus input after Cody responds
4. **Color contrast** - Violet on dark meets WCAG AA
5. **Reduced motion** - Option to disable streaming animation

---

## Technical Implementation Notes

### Files Created (Current Implementation)

Nova follows a **feature-based organization** where each major capability has its own folder with components, hooks, and types. Shared/common elements live at the nova root.

```
apps/frontend/src/features/nova/
│
├── NovaLayout.tsx            # Two-panel layout (sidebar + outlet, fixed height)
├── NovaSidebar.tsx           # Sidebar (conversations + paths preview)
├── types.ts                  # Shared Nova types (Conversation, Message, etc.)
├── index.ts                  # Barrel exports for external use
│
├── coach/                    # COACHING/CHAT FEATURE (/nova/c/*)
│   ├── NovaChatPage.tsx      # Chat page (handles new + existing convos)
│   ├── index.ts              # Barrel exports
│   ├── components/
│   │   ├── ChatArea.tsx          # Main chat container + empty state logic
│   │   ├── EmptyState.tsx        # Cody introduction (shown when no messages)
│   │   ├── StarterCards.tsx      # 4 capability showcase cards
│   │   ├── MessageList.tsx       # Scrollable message container
│   │   ├── CodyMessage.tsx       # Cody's message bubble with copy
│   │   ├── UserMessage.tsx       # User's bubble with profile image/name
│   │   ├── ChatInput.tsx         # Auto-expanding input with send
│   │   └── StreamingIndicator.tsx # "Cody is thinking..." animation
│   └── hooks/
│       ├── useChat.ts            # Chat state + mock responses
│       └── useConversations.ts   # Conversation list (mock data)
│
└── learning/                 # LEARNING PATHS FEATURE (/nova/paths/*) - FUTURE
    └── (not yet implemented - deferred to M8)
```

**Key decisions:**
- No separate `home/` folder - empty state lives in `coach/components/`
- `/nova` redirects to `/nova/c/new` (no NovaHomePage)
- EmptyState and StarterCards shown in ChatArea when no messages exist

### Feature Ownership

| Feature | Route | Responsibility |
|---------|-------|----------------|
| **Root** | - | Layout, sidebar, shared types |
| **home** | `/nova` | Welcome experience, onboarding |
| **coach** | `/nova/c/*` | All chat/conversation functionality |
| **learning** | `/nova/paths/*` | Learning paths, progress tracking |

### Delete/Archive Old Files

```
apps/frontend/src/features/twin/  # Archive or delete entirely
```

### Routing Configuration (App.tsx) - Current Implementation

```tsx
// Nova routes - protected, redirects to home if unauthenticated
<Route
  path="/"
  element={
    <ProtectedRoute redirectTo="/">
      <ShellLayout theme={theme} onThemeToggle={toggleTheme} />
    </ProtectedRoute>
  }
>
  <Route path="nova" element={<NovaLayout />}>
    {/* Redirect /nova to /nova/c/new */}
    <Route index element={<Navigate to="/nova/c/new" replace />} />
    {/* Chat routes */}
    <Route path="c/new" element={<NovaChatPage />} />
    <Route path="c/:conversationId" element={<NovaChatPage />} />
  </Route>
</Route>
```

**URL Examples:**
- `/nova` → Redirects to `/nova/c/new`
- `/nova/c/new` → New conversation with empty state + starter cards
- `/nova/c/abc123` → Existing conversation
- `/nova/paths` → (Future - M8)

**Access Control:**
- Requires authentication (redirects to `/` if not logged in)
- Hidden from App Switcher until public launch
- Users who know the URL can still access it

### Types by Feature

**Shared Types (`nova/types.ts`):**
```typescript
// Used across multiple features
export interface Conversation {
  id: string;
  title: string;
  createdAt: string;
  lastMessageAt: string;
  messageCount: number;
  topicEmoji?: string;
}

export interface Message {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
  createdAt: string;
}
```

**Learning Types (`nova/learning/types.ts`):**
```typescript
export interface LearningPath {
  id: string;
  title: string;
  description: string;
  progress: number; // 0-100
  completedTopics: number;
  totalTopics: number;
  topics: LearningTopic[];
}

export interface LearningTopic {
  id: string;
  title: string;
  status: "not_started" | "in_progress" | "completed";
  order: number;
}
```

---

## Milestone 1 Scope (MVP)

For the initial implementation, focus on:

### Must Have - Shared (`nova/`)
- [x] Nova route in App.tsx with nested routes
- [x] `NovaLayout.tsx` - Two-panel layout with Outlet (fixed height, independent scroll)
- [x] `NovaSidebar.tsx` - Conversation list + paths preview
- [x] `types.ts` - Conversation, Message interfaces
- [x] `index.ts` - Barrel exports

### Must Have - Coach Feature (`nova/coach/`)
- [x] `NovaChatPage.tsx` - Chat page (handles new + existing)
- [x] `ChatArea.tsx` - Main chat container with empty state
- [x] `EmptyState.tsx` - Cody introduction (in coach/components/)
- [x] `StarterCards.tsx` - 4 capability cards (in coach/components/)
- [x] `MessageList.tsx` - Scrollable messages
- [x] `CodyMessage.tsx` - Cody's bubble with copy button
- [x] `UserMessage.tsx` - User's bubble with profile image/name
- [x] `ChatInput.tsx` - Auto-expanding input with send
- [x] `StreamingIndicator.tsx` - "Cody is thinking..."
- [x] `useChat.ts` - Chat state management (mock responses)
- [x] `useConversations.ts` - Conversation list (mock data)

### Implementation Decisions Made
- [x] `/nova` redirects to `/nova/c/new` (no separate home page for MVP)
- [x] Empty state + starter cards live in ChatArea, shown when no messages
- [x] Sidebar shows flat list of 5 recent chats (no time groupings)
- [x] No emoji icons on conversation list items
- [x] User messages show profile image and actual name (not "You")
- [x] Nova routes require authentication (redirect to `/` if not logged in)
- [x] Nova hidden from App Switcher until public launch

### Nice to Have (M1)
- [ ] `FollowUpSuggestions.tsx` - Related questions
- [x] Message copy button (implemented in CodyMessage)
- [ ] Conversation rename/delete in sidebar

### Deferred to Later Milestones
- [ ] `nova/learning/` - Entire feature (M8)
- [ ] Attach code snippets
- [ ] Search conversations
- [ ] Feedback buttons (thumbs up/down)
- [ ] Real API integration (currently using mock responses)

---

## Open Questions for Discussion

1. **Cody vs Nova naming** - Is "Cody" the right name? Alternatives: Nova itself, Archie (architect), Max (learning), Guide
2. **Sidebar default state** - Should sidebar be collapsed by default on first visit to maximize chat space?
3. **Conversation titles** - Auto-generate from first message or let user name them?
4. **Empty state frequency** - Show welcome every time or only for new users?
5. **Mobile priority** - Is mobile important for Nova or primarily desktop?

---

## Research Sources

- [Eleken: Chatbot UI Examples](https://www.eleken.co/blog-posts/chatbot-ui-examples)
- [NN/G: UX of AI - Lessons from Perplexity](https://www.nngroup.com/articles/perplexity-henry-modisett/)
- [NN/G: Prompt Controls in GenAI Chatbots](https://www.nngroup.com/articles/prompt-controls-genai/)
- [ShapeofAI: AI UX Patterns - References](https://www.shapeof.ai/patterns/references)
- [WillowTree: 7 UX/UI Rules for Conversational AI](https://www.willowtreeapps.com/insights/willowtrees-7-ux-ui-rules-for-designing-a-conversational-ai-assistant)
- [Jotform: Best Chatbot UIs 2025](https://www.jotform.com/ai/agents/best-chatbot-ui/)
- [ChatGPT Sidebar Redesign Guide](https://www.ai-toolbox.co/chatgpt-management-and-productivity/chatgpt-sidebar-redesign-guide)
- [PatternFly: Chatbot Conversation History](https://www.patternfly.org/patternfly-ai/chatbot/chatbot-conversation-history/)
- [Mockplus: Gamification in UI/UX Guide](https://www.mockplus.com/blog/post/gamification-ui-ux-design-guide)
- [Shakuro: E-Learning App Design Guide](https://shakuro.com/blog/e-learning-app-design-and-how-to-make-it-better)
- [iPullRank: AI Search Architecture Deep Dive](https://ipullrank.com/ai-search-manual/search-architecture)
- [ByteByteGo: How Perplexity Built an AI Google](https://blog.bytebytego.com/p/how-perplexity-built-an-ai-google)

---

## Next Steps

1. ~~Review this proposal together~~ Done
2. ~~Decide on coach name (Cody or alternative)~~ Cody approved
3. ~~Approve layout direction~~ Two-panel chat layout approved
4. ~~Start implementation with NovaLayout + EmptyState~~ Done
5. **Next: Wire to backend once chat endpoint exists (Milestone 1 backend)**

### What's Ready
- Complete MVP UI with mock responses
- All core components implemented
- Protected routes with soft launch configuration
- Ready for backend integration

### What's Needed for Backend Integration
- Replace `useChat.ts` mock responses with real API calls
- Replace `useConversations.ts` mock data with real conversation history
- Implement streaming response handling
- Add error states and loading indicators
