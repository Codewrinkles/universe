# Nova: What's Next

> **Purpose**: Clear, prioritized list of what to build next for Nova.
> **Last Updated**: December 22, 2025

---

## Current State

| Component | Status | Notes |
|-----------|--------|-------|
| Frontend MVP | Done | Chat UI, settings, sidebar |
| Chat Backend (M1) | Done | Conversations, messages, streaming |
| User Profile (Phase 1) | Done | LearnerProfile, injected into every prompt |
| Memory System (Phase 2) | Done | Extraction, semantic search, injected into every prompt |
| Alpha Launch System | Done | Applications, approval, redemption |
| Admin Metrics | Done | Dashboard with usage stats |
| **Agentic RAG** | **Not Started** | Multi-source retrieval with SK Plugins |
| **Skill Tracking (Phase 3)** | **Not Started** | Key differentiator |
| **Payments/Billing** | **Not Started** | Required for Beta |

**Alpha Status**: 18 applications, targeting 50

---

## The Priority Order

```
┌─────────────────────────────────────────────────────────────────┐
│  PRIORITY 1: Fill Alpha to 50 Users                             │
│  ─────────────────────────────────────────────────────────────  │
│  YouTube (31k subs) is untapped. One video could get 50+ apps.  │
│                                                                 │
│  Effort: 1 day (recording + editing)                            │
│  Impact: HIGH - fills Alpha                                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  PRIORITY 2: Agentic RAG with SK Plugins                        │
│  ─────────────────────────────────────────────────────────────  │
│  Multi-source retrieval: official docs, books, your content,    │
│  Pulse. Nova decides which source to query.                     │
│                                                                 │
│  Effort: 2-3 weeks                                              │
│  Impact: HIGH - coaching accuracy + authority                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  PRIORITY 3: Skill Tracking (Phase 3)                           │
│  ─────────────────────────────────────────────────────────────  │
│  Nova knows what you know and adapts explanations.              │
│                                                                 │
│  Effort: 2-3 weeks                                              │
│  Impact: Key differentiator                                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  PRIORITY 4: Payments Infrastructure                            │
│  ─────────────────────────────────────────────────────────────  │
│  Stripe integration for Beta monetization.                      │
│                                                                 │
│  Effort: 1-2 weeks                                              │
│  Impact: Unlocks Beta                                           │
└─────────────────────────────────────────────────────────────────┘
```

---

## Priority 1: Fill Alpha - YouTube Video

**Why first?** Your 31k YouTube subscribers haven't been reached yet. One video could generate 50+ applications and fill Alpha.

### Video Structure (7-10 min)

| Timestamp | Section | Content |
|-----------|---------|---------|
| 0:00-0:30 | Hook | "What if your AI assistant remembered everything you've discussed?" |
| 0:30-2:00 | Problem | Current AI tools are stateless, generic, don't adapt |
| 2:00-6:00 | Demo | Show Nova: profile setup → personalized response → memory in action |
| 6:00-7:00 | Vision | Where Nova is going (skill tracking, learning paths) |
| 7:00-end | CTA | "I'm looking for 50 developers to test this. Link in description." |

**Key**: This isn't a sales pitch. You're sharing what you're building with your community.

---

## Priority 2: Agentic RAG with SK Plugins (2-3 weeks)

**Why?** Nova needs to be grounded in authoritative sources, not just base LLM knowledge. Multiple sources with intelligent routing.

### Content Sources (by authority)

| Source | Authority | How to Get |
|--------|-----------|------------|
| Official docs | Highest | Scrape Microsoft Learn, React docs, etc. |
| Books (summaries) | High | Your summaries of Evans, Fowler, etc. |
| Expert blogs | Medium-High | Scrape with attribution |
| Your YouTube | Medium | Extract transcripts |
| Pulse posts | Context | Already in DB |

### Architecture

```
User: "How should I structure aggregates in DDD?"
                    │
                    ▼
┌─────────────────────────────────────────────────────────────────┐
│  Semantic Kernel with Plugins                                    │
│                                                                  │
│  Nova decides: "This is DDD → search books first"               │
│                                                                  │
│  Available tools:                                                │
│  ├── search_official_docs(query, technology)                    │
│  ├── search_books(query, authors[])                             │
│  ├── search_youtube(query)                                      │
│  └── search_pulse(query)                                        │
└─────────────────────────────────────────────────────────────────┘
                    │
                    ▼
        Calls search_books("aggregates", ["Eric Evans"])
                    │
                    ▼
        Returns relevant chunks from DDD book summary
                    │
                    ▼
        Nova responds with grounded, authoritative answer
```

### Implementation Plan

#### Week 1: Infrastructure

| Task | Details |
|------|---------|
| Add Semantic Kernel | `Microsoft.SemanticKernel` NuGet package |
| Create plugin base | `INovaPlugin` interface, registration |
| Content entity | `ContentChunk` with source, embedding, metadata |
| Ingestion pipeline | Chunk → embed → store |

#### Week 2: First Sources

| Task | Details |
|------|---------|
| YouTube ingestion | Extract transcripts, chunk, embed |
| Book summaries | Ingest your DDD/architecture summaries |
| `SearchContentPlugin` | First SK plugin with tool functions |
| Update SendMessage | Use SK instead of raw LLM calls |

#### Week 3: Expand + Polish

| Task | Details |
|------|---------|
| Official docs | Ingest Microsoft Learn for .NET/C# |
| Pulse integration | Query existing Pulse posts |
| Routing logic | Nova picks right source |
| Testing | Verify answer quality improvement |

### Entities

```csharp
// Content chunk for RAG
public class ContentChunk
{
    public Guid Id { get; private set; }
    public ContentSource Source { get; private set; }  // OfficialDocs, Book, YouTube, Pulse
    public string SourceIdentifier { get; private set; } // URL, video ID, book title
    public string Title { get; private set; }
    public string Content { get; private set; }
    public byte[] Embedding { get; private set; }
    public int TokenCount { get; private set; }
    public Dictionary<string, string> Metadata { get; private set; } // author, date, etc.
    public DateTimeOffset CreatedAt { get; private set; }
}

public enum ContentSource
{
    OfficialDocs,
    Book,
    YouTube,
    Pulse,
    Article
}
```

### SK Plugin Example

```csharp
public class SearchContentPlugin
{
    private readonly IContentRepository _repository;
    private readonly IEmbeddingService _embeddingService;

    [KernelFunction("search_books")]
    [Description("Search book summaries for concepts. Use for DDD, architecture, patterns.")]
    public async Task<string> SearchBooks(
        [Description("The search query")] string query,
        [Description("Optional author filter")] string[]? authors = null)
    {
        var embedding = await _embeddingService.GetEmbeddingAsync(query);
        var chunks = await _repository.SearchAsync(
            embedding,
            source: ContentSource.Book,
            authors: authors,
            limit: 5);

        return FormatChunksForContext(chunks);
    }

    [KernelFunction("search_official_docs")]
    [Description("Search official documentation. Use for API references, language features.")]
    public async Task<string> SearchOfficialDocs(
        [Description("The search query")] string query,
        [Description("Technology: dotnet, react, typescript, etc.")] string technology)
    {
        // Similar implementation
    }
}
```

### Success Criteria

- [ ] Nova cites authoritative sources in responses
- [ ] DDD questions reference Evans/Vernon concepts correctly
- [ ] .NET questions align with Microsoft docs
- [ ] Answers are consistent and accurate
- [ ] Users notice improved quality

---

## Priority 3: Skill Tracking (2-3 weeks)

**Why?** Key differentiator. Nova knows what you know and adapts explanations accordingly.

### What It Enables

```
User: "How do I query a database?"

Nova thinks: User has mastered LINQ basics (85%), hasn't seen GroupBy (10%)
Nova responds: "You're solid with Where/Select. Let me show you GroupBy
               which you haven't used yet..."
```

### What to Build

1. **Concept taxonomy** (skill hierarchy):
   ```
   csharp/
   ├── basics/
   ├── linq/
   │   ├── basics
   │   ├── advanced
   │   └── expression-trees
   ├── async/
   └── ...
   ```

2. **UserSkillState** entity:
   - `MasteryProbability` (0.0 to 1.0)
   - Bayesian updates from conversation signals

3. **Skill signal extraction**:
   - User demonstrated understanding → increase mastery
   - User expressed confusion → decrease mastery

4. **Skill-aware prompting**:
   - Don't over-explain mastered topics
   - Identify gaps proactively

### Reference
See `docs/nova-skill-tracking-plan.md` for detailed implementation.

---

## Priority 4: Payments Infrastructure (1-2 weeks)

**Why?** Required for Beta monetization.

### What to Build

1. **Billing schema** (EF Core entities):
   - `Customer` (links to Identity)
   - `Subscription` (Stripe subscription mirror)
   - `PaymentMethod` (cards on file)
   - `Invoice` (payment history)

2. **Stripe integration**:
   - Checkout session creation
   - Webhook handling (subscription events)
   - Customer portal redirect

3. **Gating logic**:
   - Free tier limits (10 messages/day)
   - Pro tier unlimited
   - Upgrade prompts in UI

4. **Settings UI** (`/settings/billing`):
   - Current plan display
   - Payment method management
   - Invoice history

### Reference
See Appendix A in `docs/alpha-to-launch-roadmap.md` for schema details.

---

## Timeline Summary

| Week | Focus | Outcome |
|------|-------|---------|
| **Week 1** | YouTube Video | 50+ Alpha applications |
| **Week 2-4** | Agentic RAG | Multi-source retrieval working |
| **Week 5-7** | Skill Tracking | Phase 3 complete |
| **Week 8-9** | Payments | Stripe integration, gating |
| **Week 9** | **BETA READY** | Can accept paying users |

---

## Decision Points

### Before Beta
- [ ] RAG content sources: Which to prioritize?
- [ ] Pricing confirmed: $12/month Pro, $199 Lifetime?
- [ ] Free tier limits confirmed: 10 messages/day?
- [ ] Founding member discount: $8/month for Alpha users?

### Before Public Launch
- [ ] All priority content sources indexed?
- [ ] Testimonials from Beta users?

---

## Quick Reference: Remaining Docs

| Document | Contains |
|----------|----------|
| `nova-next-steps.md` | This file - what to do next |
| `nova-personalization-roadmap.md` | Phase 3-4 technical details |
| `nova-skill-tracking-plan.md` | Skill tracking implementation |
| `nova-plugins-plan.md` | SK Plugins reference (now Priority 2) |
| `alpha-to-launch-roadmap.md` | GTM strategy + payments schema |

---

**Path to Beta**: YouTube → Agentic RAG → Skill Tracking → Payments → Beta
