# Nova Implementation Plan

> **Last Updated**: 2024-12-14
> **Status**: MVP UI Implemented (Frontend only, backend pending)

---

## The Vision

**Nova = The go-to AI coach for technical learning**

People don't Google anymore. They chat. Nova becomes the ChatGPT for software development, architecture, and technical decision-making - powered by curated, authoritative content from multiple sources.

### Core Value Proposition

- **Chat-first learning**: Users start a conversation, not a search
- **Multi-source knowledge**: YouTube, Substack, Pulses, public domain books, documentation, and future sources
- **AI coaching**: Not just answers, but guidance, explanations, and learning paths
- **On-the-fly roadmaps**: Personalized learning paths generated from conversation analysis
- **Language/framework agnostic**: All technical topics, not limited to one stack

### Target Use Cases

1. "How do I implement Clean Architecture in .NET?"
2. "Explain the difference between microservices and modular monoliths"
3. "I'm building a SaaS app - what architectural decisions should I consider?"
4. "Create a learning path for me to become a better software architect"
5. "What does Dan from Codewrinkles think about vertical slice architecture?"

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     CONTENT SOURCES                              │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐   │
│  │ YouTube │ │Substack │ │ Pulses  │ │  Books  │ │ Future  │   │
│  │Transcr. │ │Articles │ │(#topic) │ │(public) │ │ Sources │   │
│  └────┬────┘ └────┬────┘ └────┬────┘ └────┬────┘ └────┬────┘   │
│       └──────────┴──────────┴──────────┴──────────┴─────┘      │
│                              │                                   │
│                              ▼                                   │
│              ┌───────────────────────────────┐                  │
│              │   Unified Ingestion Pipeline   │                  │
│              │   (Source → Chunk → Embed)     │                  │
│              └───────────────────────────────┘                  │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                    KNOWLEDGE LAYER                               │
│              ┌───────────────────────────────┐                  │
│              │      Azure AI Search          │                  │
│              │   (vectors + metadata)        │                  │
│              └───────────────────────────────┘                  │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      NOVA CORE                                   │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│  │  Retrieval  │  │   Context   │  │    Response Generator   │ │
│  │   Engine    │→ │   Builder   │→ │  (coaching personality) │ │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘ │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │              Learning Roadmap Engine                        ││
│  │  (analyze conversation → detect gaps → suggest path)        ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                         CHAT UI                                  │
│            User asks → Nova responds → Sources shown            │
│                    → Learning path suggested                     │
└─────────────────────────────────────────────────────────────────┘
```

---

## Technology Decisions

| Component | Choice | Rationale |
|-----------|--------|-----------|
| **LLM Provider** | OpenAI | Model flexibility (gpt-4o-mini, GPT-4/5, embeddings, Codex) |
| **Vector Database** | Azure AI Search | Managed, .NET friendly, hybrid search, already on Azure |
| **Embedding Model** | OpenAI text-embedding-3-small | Good balance of quality/cost |
| **Backend** | .NET 10, Clean Architecture | Existing stack |
| **Frontend** | React (existing TwinPage) | UI mockup already exists |

---

## Milestones

### Milestone 1: Nova Can Chat (No RAG)

**Goal**: Working AI coach that responds. No knowledge base yet - pure LLM.

**Outcome**: End-to-end conversation flow works. Nova has a coaching personality.

#### Frontend (Complete)
| Step | Task | Size | Status |
|------|------|------|--------|
| 1.F1 | Create NovaLayout with two-panel layout | M | [x] |
| 1.F2 | Create NovaSidebar with conversation list | M | [x] |
| 1.F3 | Create NovaChatPage with chat components | M | [x] |
| 1.F4 | Create EmptyState with Cody introduction | S | [x] |
| 1.F5 | Create StarterCards (4 capability prompts) | S | [x] |
| 1.F6 | Create CodyMessage and UserMessage components | M | [x] |
| 1.F7 | Create ChatInput with auto-expand | S | [x] |
| 1.F8 | Create useChat hook with mock responses | M | [x] |
| 1.F9 | Add protected routes + hide from App Switcher | S | [x] |

#### Backend (Pending)
| Step | Task | Size | Status |
|------|------|------|--------|
| 1.1 | Create `ConversationSession` domain entity | S | [ ] |
| 1.2 | Create `Message` domain entity with `Role` enum | S | [ ] |
| 1.3 | Create EF Core configurations + migration | S | [ ] |
| 1.4 | Create `ILlmService` interface | S | [ ] |
| 1.5 | Implement `OpenAIService` (chat completion) | M | [ ] |
| 1.6 | Write Nova system prompt (coaching personality) | M | [ ] |
| 1.7 | Create `SendMessageCommand` handler | M | [ ] |
| 1.8 | Create `POST /api/nova/chat` endpoint | S | [ ] |

#### Integration (Pending)
| Step | Task | Size | Status |
|------|------|------|--------|
| 1.9 | Replace useChat mock with real API calls | M | [ ] |
| 1.10 | Implement streaming response handling | M | [ ] |
| 1.11 | Replace useConversations mock with real data | S | [ ] |

**Ship Checkpoint**: Nova chats with users!

---

### Milestone 2: Nova Can Retrieve (Basic RAG)

**Goal**: Add ability to search a knowledge base. Start with manually indexed content.

**Outcome**: Nova answers using retrieved knowledge. Sources shown.

| Step | Task | Size | Status |
|------|------|------|--------|
| 2.1 | Set up Azure AI Search resource | S | [ ] |
| 2.2 | Define search index schema | S | [ ] |
| 2.3 | Create `IEmbeddingService` interface | S | [ ] |
| 2.4 | Implement embedding service (OpenAI) | S | [ ] |
| 2.5 | Create `ISearchService` interface | S | [ ] |
| 2.6 | Implement `AzureSearchService` | M | [ ] |
| 2.7 | Create `IndexedContent` domain entity | S | [ ] |
| 2.8 | Integrate retrieval into `SendMessage` handler | M | [ ] |
| 2.9 | Add source attribution to responses | S | [ ] |
| 2.10 | Index test content manually (10-20 chunks) | S | [ ] |

**Ship Checkpoint**: RAG pipeline works end-to-end!

---

### Milestone 3: Content Ingestion Abstraction

**Goal**: Build pipeline that any content source can plug into.

**Outcome**: Unified ingestion flow. First source: manual text.

| Step | Task | Size | Status |
|------|------|------|--------|
| 3.1 | Create `IContentSource` interface | S | [ ] |
| 3.2 | Create `IChunkingStrategy` interface | S | [ ] |
| 3.3 | Create `ContentChunk` value object | S | [ ] |
| 3.4 | Create `IngestionPipeline` service | M | [ ] |
| 3.5 | Create `ContentSource` domain entity | S | [ ] |
| 3.6 | Create admin endpoint to trigger ingestion | S | [ ] |
| 3.7 | Implement `ManualTextSource` | S | [ ] |

**Ship Checkpoint**: Can add content via text paste!

---

### Milestone 4: First Real Content Source

**Goal**: Implement one real content source end-to-end.

**Option A: Pulses**
| Step | Task | Size | Status |
|------|------|------|--------|
| 4.1 | Implement `PulseContentSource` | M | [ ] |
| 4.2 | Implement pulse chunking (if needed) | S | [ ] |
| 4.3 | Index pulses with specific hashtags | S | [ ] |
| 4.4 | Test retrieval quality | S | [ ] |

**Option B: YouTube Transcripts**
| Step | Task | Size | Status |
|------|------|------|--------|
| 4.1 | Implement `YouTubeContentSource` | M | [ ] |
| 4.2 | Implement transcript chunking | M | [ ] |
| 4.3 | Index video transcripts | S | [ ] |
| 4.4 | Add timestamp attribution | S | [ ] |

**Ship Checkpoint**: Real content searchable!

---

### Milestone 5: Authority & Quality System

**Goal**: Not all content is equal. Add authority weighting.

| Step | Task | Size | Status |
|------|------|------|--------|
| 5.1 | Add `AuthorityLevel` to indexed content | S | [ ] |
| 5.2 | Add `ContentCategory` enum | S | [ ] |
| 5.3 | Implement authority-weighted retrieval | M | [ ] |
| 5.4 | Add recency boosting | S | [ ] |
| 5.5 | Update system prompt with authority guidelines | S | [ ] |

**Ship Checkpoint**: Nova prefers authoritative, recent content!

---

### Milestone 6: Additional Content Sources

**Goal**: Prove the abstraction works. Add more sources.

| Source | Priority | Notes |
|--------|----------|-------|
| YouTube Transcripts | High | If not done in M4 |
| Substack Articles | High | Written content |
| Pulses by Hashtag | Medium | Community content |
| Public Domain Books | Medium | Classic CS/architecture books |
| Official Documentation | Low | Microsoft Learn, etc. |
| GitHub READMEs | Low | Project documentation |

Each source follows the same pattern:
1. Implement `IContentSource`
2. Implement appropriate `IChunkingStrategy`
3. Index content
4. Test retrieval

---

### Milestone 7: Conversation Memory

**Goal**: Nova remembers what you discussed.

| Step | Task | Size | Status |
|------|------|------|--------|
| 7.1 | Store full conversation history | S | [ ] |
| 7.2 | Implement conversation summarization | M | [ ] |
| 7.3 | Add user profile/preferences | M | [ ] |
| 7.4 | Context-aware retrieval | M | [ ] |

**Ship Checkpoint**: Nova maintains context across messages!

---

### Milestone 8: Learning Roadmaps (Differentiator)

**Goal**: Nova can create personalized learning paths.

| Step | Task | Size | Status |
|------|------|------|--------|
| 8.1 | Create `LearningPath` domain entity | S | [ ] |
| 8.2 | Implement knowledge gap detection | M | [ ] |
| 8.3 | Implement roadmap generation prompt | M | [ ] |
| 8.4 | Add "Create learning path" chat action | S | [ ] |
| 8.5 | Track progress on roadmap | M | [ ] |
| 8.6 | Proactive next-topic suggestions | M | [ ] |

**Ship Checkpoint**: Personalized learning paths from conversations!

---

## Domain Model (Draft)

### Core Entities

```csharp
// Conversation management
ConversationSession
├── Id: Guid
├── ProfileId: Guid
├── Title: string
├── CreatedAt: DateTimeOffset
├── LastMessageAt: DateTimeOffset
└── Messages: List<Message>

Message
├── Id: Guid
├── SessionId: Guid
├── Role: MessageRole (User/Assistant/System)
├── Content: string
├── CreatedAt: DateTimeOffset
├── TokensUsed: int?
├── ModelUsed: string?
└── SourceReferences: List<SourceReference>

// Content indexing
IndexedContent
├── Id: Guid
├── SourceType: ContentSourceType
├── SourceId: string
├── SourceUrl: string
├── Title: string
├── AuthorityLevel: int (1-5)
├── Category: ContentCategory
├── PublishedAt: DateTimeOffset
├── IndexedAt: DateTimeOffset
└── ChunkCount: int

ContentSource
├── Id: Guid
├── Type: ContentSourceType
├── Name: string
├── Configuration: JsonDocument
├── LastIngestionAt: DateTimeOffset?
└── IsActive: bool

// Learning paths (Milestone 8)
LearningPath
├── Id: Guid
├── ProfileId: Guid
├── Title: string
├── Description: string
├── GeneratedFromSessionId: Guid?
├── CreatedAt: DateTimeOffset
└── Steps: List<LearningStep>

LearningStep
├── Id: Guid
├── PathId: Guid
├── Order: int
├── Topic: string
├── Description: string
├── Status: LearningStatus (NotStarted/InProgress/Completed)
└── RelatedContentIds: List<Guid>
```

### Enums

```csharp
MessageRole: User, Assistant, System
ContentSourceType: Manual, YouTube, Substack, Pulse, Book, Documentation
ContentCategory: OfficialDocs, CreatorContent, CommunityContent, BookContent
LearningStatus: NotStarted, InProgress, Completed
```

---

## API Endpoints (Draft)

### Chat
```
POST /api/nova/chat
{
  "sessionId": "guid" | null,
  "message": "string"
}
→ {
  "sessionId": "guid",
  "response": "string",
  "sources": [{ title, url, type }]
}

GET /api/nova/sessions
→ { sessions: [...] }

GET /api/nova/sessions/{id}
→ { session with messages }

DELETE /api/nova/sessions/{id}
```

### Content (Admin)
```
POST /api/nova/admin/content/ingest
{
  "sourceType": "Manual",
  "content": "string",
  "title": "string",
  "authorityLevel": 3
}

GET /api/nova/admin/content/sources
POST /api/nova/admin/content/sources/{type}/sync
```

### Learning Paths (Milestone 8)
```
POST /api/nova/paths/generate
{
  "sessionId": "guid"
}

GET /api/nova/paths
GET /api/nova/paths/{id}
PATCH /api/nova/paths/{id}/steps/{stepId}/status
```

---

## Chunking Strategies

Different content types need different chunking approaches:

| Content Type | Strategy | Chunk Size | Notes |
|--------------|----------|------------|-------|
| **Pulses** | None or minimal | Full pulse | Already short-form |
| **Substack** | Heading-based + paragraph | ~600 tokens | Respect document structure |
| **YouTube** | Semantic + timestamp | ~500-800 tokens | Anchor to timestamps |
| **Books** | Chapter/section-based | ~800 tokens | Preserve chapter context |
| **Documentation** | Section-based | ~600 tokens | Keep code blocks intact |

All strategies should include:
- **Overlap**: 10-20% to prevent hard cutoffs
- **Metadata**: Source info, position, timestamps where applicable

---

## System Prompt (Draft)

```markdown
You are Nova, an AI learning coach for software development and architecture.

## Your Role
- Help developers learn and grow in their technical skills
- Provide clear, accurate explanations of complex concepts
- Guide users through architectural decisions
- Create personalized learning paths based on their goals and current knowledge

## Guidelines
- Be encouraging but honest - acknowledge complexity when it exists
- Use retrieved context to ground your responses in authoritative sources
- Always cite your sources when using retrieved content
- If you don't have relevant information, say so rather than making things up
- Adapt your explanations to the user's apparent experience level
- When multiple valid approaches exist, present them fairly with trade-offs

## Authority Hierarchy
When multiple sources conflict, prefer (in order):
1. Official documentation
2. Recent creator content (< 6 months)
3. Established books/courses
4. Community content
5. Older content

## Response Format
- Start with a direct answer or acknowledgment
- Provide explanation with supporting details
- Include code examples when relevant
- End with sources used (if any retrieved content was used)
- Optionally suggest related topics or next steps
```

---

## Open Questions

1. **Conversation limits**: How long can a conversation be before we need to summarize?
2. **Rate limiting**: How to handle API costs? Per-user limits?
3. **Content freshness**: How often to re-index sources?
4. **Multi-tenancy**: Is Nova shared or per-user content possible?
5. **Feedback loop**: How do users report bad responses? How does that improve the system?

---

## Files to Create

```
apps/backend/src/
├── Codewrinkles.Domain/Nova/
│   ├── ConversationSession.cs
│   ├── Message.cs
│   ├── MessageRole.cs
│   ├── IndexedContent.cs
│   ├── ContentSource.cs
│   ├── ContentSourceType.cs
│   ├── ContentCategory.cs
│   ├── LearningPath.cs (M8)
│   └── LearningStep.cs (M8)
│
├── Codewrinkles.Application/Nova/
│   ├── SendMessage.cs
│   ├── GetSession.cs
│   ├── GetSessions.cs
│   ├── DeleteSession.cs
│   ├── Prompts/
│   │   ├── SystemPrompt.md
│   │   └── RetrievalPrompt.md
│   └── Interfaces/
│       ├── ILlmService.cs
│       ├── IEmbeddingService.cs
│       ├── ISearchService.cs
│       ├── IContentSource.cs
│       └── IChunkingStrategy.cs
│
├── Codewrinkles.Infrastructure/Nova/
│   ├── OpenAIService.cs
│   ├── AzureSearchService.cs
│   ├── IngestionPipeline.cs
│   ├── ContentSources/
│   │   ├── ManualTextSource.cs
│   │   ├── PulseContentSource.cs
│   │   └── YouTubeContentSource.cs
│   └── ChunkingStrategies/
│       ├── ParagraphChunkingStrategy.cs
│       └── SemanticChunkingStrategy.cs
│
├── Codewrinkles.Infrastructure/Persistence/Configurations/Nova/
│   ├── ConversationSessionConfiguration.cs
│   ├── MessageConfiguration.cs
│   ├── IndexedContentConfiguration.cs
│   └── ContentSourceConfiguration.cs
│
└── Codewrinkles.API/Modules/Nova/
    └── NovaEndpoints.cs
```

---

## Current Status

**Frontend MVP is complete** with mock responses. Nova is accessible at `/nova` for authenticated users who know the URL (hidden from App Switcher for soft launch).

## Next Session

Ready to start with **Milestone 1 Backend, Step 1.1**: Create `ConversationSession` domain entity.

Or discuss:
- System prompt refinement (Cody's coaching personality)
- OpenAI integration approach
- Streaming response implementation
- Any architectural concerns
