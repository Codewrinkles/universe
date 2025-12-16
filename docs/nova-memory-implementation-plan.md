# Nova Memory System - Implementation Plan

> **Status**: Draft
> **Created**: 2024-12-16
> **Purpose**: Detailed implementation plan for conversation memory extraction and injection

---

## Overview

This plan implements conversation memory extraction and injection for personalized coaching. Following the agreed approach:

- **Trigger**: On new conversation start, process previous unprocessed sessions
- **Storage**: SQL Server with embeddings stored as `varbinary(max)`
- **Injection**: Recent memories + important + semantically relevant
- **Conflict handling**: Category-based with single/multi cardinality

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| When to extract | On new chat start + process from `LastProcessedMessageId` | Handles users who return to old chats |
| How many to inject | 15-20 max (recent + important + semantic) | Balance context richness with token budget |
| Conflict handling | Category-based cardinality | Simple, handles contradictions naturally |
| Embedding storage | SQL Server `varbinary(max)` | Keep it simple, similarity calc in app layer |
| Repository structure | Separate `INovaMemoryRepository` | Avoid God class in `INovaRepository` |

---

## Part 1: Domain Layer Changes

### 1.1 New Enum: `MemoryCategory.cs`

**Location**: `Codewrinkles.Domain/Nova/MemoryCategory.cs`

```csharp
namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Categories of memories extracted from conversations.
/// Each category has an associated cardinality rule.
/// </summary>
public enum MemoryCategory
{
    /// <summary>
    /// Topics the user asked about. (Multi)
    /// </summary>
    TopicDiscussed = 0,

    /// <summary>
    /// Concepts Cody explained to the user. (Multi)
    /// </summary>
    ConceptExplained = 1,

    /// <summary>
    /// Areas where user showed confusion or difficulty. (Multi)
    /// </summary>
    StruggleIdentified = 2,

    /// <summary>
    /// Areas where user demonstrated competence. (Multi)
    /// </summary>
    StrengthDemonstrated = 3,

    /// <summary>
    /// Specific questions the user asked. (Multi)
    /// </summary>
    QuestionAsked = 4,

    /// <summary>
    /// What user is currently working on or learning. (Single - new supersedes old)
    /// </summary>
    CurrentFocus = 5,

    /// <summary>
    /// What kind of examples resonate with the user. (Single - new supersedes old)
    /// </summary>
    PreferredExamples = 6
}
```

### 1.2 New Entity: `Memory.cs`

**Location**: `Codewrinkles.Domain/Nova/Memory.cs`

**Properties**:
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | PK, sequential generation |
| `ProfileId` | `Guid` | FK → identity.Profiles |
| `SourceSessionId` | `Guid` | FK → nova.ConversationSessions |
| `Category` | `MemoryCategory` | Type of memory |
| `Content` | `string` | The memory text (max 1000 chars) |
| `Embedding` | `byte[]?` | Serialized float[] for semantic search |
| `Importance` | `int` | 1-5 scale, default 3 |
| `OccurrenceCount` | `int` | For accumulating patterns, default 1 |
| `CreatedAt` | `DateTimeOffset` | When memory was created |
| `SupersededAt` | `DateTimeOffset?` | When this memory was replaced |
| `SupersededById` | `Guid?` | FK → nova.Memories (self-reference) |

**Navigation Properties**:
- `Profile`: Profile
- `SourceSession`: ConversationSession
- `SupersededBy`: Memory?

**Factory Method**:
```csharp
public static Memory Create(
    Guid profileId,
    Guid sourceSessionId,
    MemoryCategory category,
    string content,
    byte[]? embedding = null,
    int importance = 3)
```

**Public Methods**:
- `Supersede(Guid newMemoryId)` - Marks this memory as superseded
- `IncrementOccurrence()` - For accumulating patterns
- `UpdateEmbedding(byte[] embedding)` - Set/update embedding

**Helper Method**:
```csharp
/// <summary>
/// Returns true if this category allows only one active memory.
/// </summary>
public static bool IsSingleCardinality(MemoryCategory category)
{
    return category is MemoryCategory.CurrentFocus or MemoryCategory.PreferredExamples;
}
```

### 1.3 Update Entity: `ConversationSession.cs`

**Add Properties**:
```csharp
/// <summary>
/// When memory was last extracted from this session.
/// Null if never extracted.
/// </summary>
public DateTimeOffset? LastMemoryExtractionAt { get; private set; }

/// <summary>
/// The ID of the last message that was processed for memory extraction.
/// Used to only process new messages on subsequent extractions.
/// </summary>
public Guid? LastProcessedMessageId { get; private set; }
```

**Add Method**:
```csharp
/// <summary>
/// Marks memory as extracted up to the specified message.
/// </summary>
public void MarkMemoryExtracted(Guid lastProcessedMessageId)
{
    LastMemoryExtractionAt = DateTimeOffset.UtcNow;
    LastProcessedMessageId = lastProcessedMessageId;
}
```

---

## Part 2: Infrastructure Layer Changes

### 2.1 New Configuration: `MemoryConfiguration.cs`

**Location**: `Codewrinkles.Infrastructure/Persistence/Configurations/Nova/MemoryConfiguration.cs`

**Key aspects**:
- Table: `nova.Memories`
- Category stored as string (like LearningStyle/PreferredPace)
- Embedding as `varbinary(max)` with no max length constraint
- Self-referential FK for SupersededById with `OnDelete(NoAction)`

**Indexes**:
```
IX_Memories_ProfileId_Category_SupersededAt
  - For querying active memories by category
  - Filtered: WHERE SupersededAt IS NULL

IX_Memories_ProfileId_CreatedAt_Desc
  - For recent memories query

IX_Memories_ProfileId_Importance_Desc
  - For high-importance memories query

IX_Memories_SourceSessionId
  - For finding memories from a session
```

### 2.2 Update Configuration: `ConversationSessionConfiguration.cs`

**Add property configurations**:
```csharp
builder.Property(c => c.LastMemoryExtractionAt)
    .IsRequired(false);

builder.Property(c => c.LastProcessedMessageId)
    .IsRequired(false);
```

**Add index**:
```csharp
// Index for finding sessions needing memory extraction
builder.HasIndex(c => new { c.ProfileId, c.LastMemoryExtractionAt })
    .HasDatabaseName("IX_ConversationSessions_ProfileId_LastMemoryExtractionAt");
```

### 2.3 Update: `ApplicationDbContext.cs`

**Add DbSet**:
```csharp
// Nova schema
public DbSet<Memory> Memories => Set<Memory>();
```

### 2.4 Migration: `AddMemorySystem`

**Creates**:
- `nova.Memories` table with all columns
- Adds `LastMemoryExtractionAt` and `LastProcessedMessageId` to `nova.ConversationSessions`
- Foreign keys and indexes as specified

---

## Part 3: Repository Layer Changes

### 3.1 New Interface: `INovaMemoryRepository.cs`

**Location**: `Codewrinkles.Application/Common/Interfaces/INovaMemoryRepository.cs`

```csharp
using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

public interface INovaMemoryRepository
{
    // Query operations

    /// <summary>
    /// Find memory by ID. Returns null if not found.
    /// </summary>
    Task<Memory?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active (non-superseded) memories for a profile.
    /// </summary>
    Task<IReadOnlyList<Memory>> GetActiveByProfileIdAsync(
        Guid profileId,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active memories by category for a profile.
    /// </summary>
    Task<IReadOnlyList<Memory>> GetActiveByCategoryAsync(
        Guid profileId,
        MemoryCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get most recent memories for a profile (by CreatedAt desc).
    /// </summary>
    Task<IReadOnlyList<Memory>> GetRecentAsync(
        Guid profileId,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get memories with embeddings for semantic search.
    /// Only returns memories that have embeddings set.
    /// </summary>
    Task<IReadOnlyList<Memory>> GetWithEmbeddingsAsync(
        Guid profileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get high-importance memories (importance >= threshold).
    /// </summary>
    Task<IReadOnlyList<Memory>> GetByMinImportanceAsync(
        Guid profileId,
        int minImportance,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find the active memory for a single-cardinality category.
    /// Returns null if no active memory exists.
    /// </summary>
    Task<Memory?> FindActiveByCategoryAsync(
        Guid profileId,
        MemoryCategory category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find similar memories by content (for deduplication).
    /// Uses exact content match - semantic similarity done in app layer.
    /// </summary>
    Task<Memory?> FindByContentAsync(
        Guid profileId,
        MemoryCategory category,
        string content,
        CancellationToken cancellationToken = default);

    // Write operations

    /// <summary>
    /// Create a new memory.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void Create(Memory memory);

    /// <summary>
    /// Update a memory.
    /// Does not save to database - call UnitOfWork.SaveChangesAsync().
    /// </summary>
    void Update(Memory memory);
}
```

### 3.2 New Implementation: `NovaMemoryRepository.cs`

**Location**: `Codewrinkles.Infrastructure/Persistence/Repositories/Nova/NovaMemoryRepository.cs`

Implements `INovaMemoryRepository` following existing repository patterns:
- Constructor takes `ApplicationDbContext`
- Uses `DbSet<Memory>` via `context.Set<Memory>()`
- Async/await pattern
- `AsNoTracking()` for read operations

### 3.3 Update Interface: `INovaRepository.cs`

**Add session operations for memory extraction**:
```csharp
/// <summary>
/// Get sessions that need memory extraction for a profile.
/// Returns sessions where LastMessageAt > LastMemoryExtractionAt (or never extracted).
/// Excludes deleted sessions.
/// </summary>
Task<IReadOnlyList<ConversationSession>> GetSessionsNeedingMemoryExtractionAsync(
    Guid profileId,
    CancellationToken cancellationToken = default);

/// <summary>
/// Get messages created after a specific message ID.
/// Used for incremental memory extraction.
/// </summary>
Task<IReadOnlyList<Message>> GetMessagesAfterAsync(
    Guid sessionId,
    Guid? afterMessageId,
    CancellationToken cancellationToken = default);
```

### 3.4 Update: `NovaRepository.cs`

Implement the two new methods.

### 3.5 Update Interface: `IUnitOfWork.cs`

**Add**:
```csharp
INovaMemoryRepository NovaMemories { get; }
```

### 3.6 Update: `UnitOfWork.cs`

Add `NovaMemories` property and inject `NovaMemoryRepository`.

---

## Part 4: Application Layer Changes

### 4.1 New Interface: `IEmbeddingService.cs`

**Location**: `Codewrinkles.Application/Common/Interfaces/IEmbeddingService.cs`

```csharp
namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Service for generating text embeddings and computing similarity.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding as a float array.</returns>
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compute cosine similarity between two embedding vectors.
    /// </summary>
    /// <param name="a">First embedding.</param>
    /// <param name="b">Second embedding.</param>
    /// <returns>Similarity score between -1 and 1 (1 = identical).</returns>
    float CosineSimilarity(float[] a, float[] b);

    /// <summary>
    /// Serialize embedding to bytes for database storage.
    /// </summary>
    byte[] SerializeEmbedding(float[] embedding);

    /// <summary>
    /// Deserialize embedding from database storage.
    /// </summary>
    float[] DeserializeEmbedding(byte[] data);
}
```

### 4.2 New CQRS: `ExtractMemories.cs`

**Location**: `Codewrinkles.Application/Nova/ExtractMemories.cs`

**Command**:
```csharp
public sealed record ExtractMemoriesCommand(
    Guid ProfileId,
    Guid SessionId
) : ICommand<ExtractMemoriesResult>;

public sealed record ExtractMemoriesResult(
    int MemoriesCreated,
    int MemoriesSuperseded,
    int MemoriesReinforced
);
```

**Handler logic**:
1. Get session by ID, verify ownership
2. Get messages after `LastProcessedMessageId` (or all if null)
3. If no new messages, return early with zeros
4. Build conversation transcript from messages
5. Call LLM with memory extraction prompt (see below)
6. Parse JSON response into memory objects
7. For each extracted memory:
   - If single-cardinality: find and supersede existing active memory
   - If multi-cardinality: check for similar existing (exact content match), increment count or create new
8. Generate embeddings for new memories
9. Save all memories
10. Update session's `MarkMemoryExtracted(lastMessageId)`
11. Return counts

**Memory Extraction Prompt**:
```
You are analyzing a conversation between a learner and an AI coach.
Extract key memories about the learner that would be useful for future conversations.

Conversation:
{transcript}

Return a JSON object with these fields (all arrays can be empty):
{
  "topics_discussed": ["topic1", "topic2"],
  "concepts_explained": ["concept1", "concept2"],
  "struggles_identified": ["struggle1"],
  "strengths_demonstrated": ["strength1"],
  "current_focus": "what they're working on" or null,
  "importance_notes": {
    "topic1": 4,
    "struggle1": 5
  }
}

Guidelines:
- Be specific and concise (each item should be 1-2 sentences max)
- Only include things actually discussed, don't infer
- Rate importance 1-5 (5 = critical to remember)
- current_focus should capture their main project/learning goal if mentioned
```

### 4.3 New CQRS: `GetMemoriesForContext.cs`

**Location**: `Codewrinkles.Application/Nova/GetMemoriesForContext.cs`

**Query**:
```csharp
public sealed record GetMemoriesForContextQuery(
    Guid ProfileId,
    string CurrentMessage
) : ICommand<GetMemoriesForContextResult>;

public sealed record GetMemoriesForContextResult(
    IReadOnlyList<MemoryDto> Memories,
    string FormattedContext
);

public sealed record MemoryDto(
    Guid Id,
    string Category,
    string Content,
    int Importance,
    DateTimeOffset CreatedAt
);
```

**Handler logic**:
1. Get recent memories (last 5 by CreatedAt)
2. Get high-importance memories (importance >= 4, limit 5)
3. Get all memories with embeddings
4. Generate embedding for current message
5. Compute cosine similarity for each memory
6. Take top 5-10 most semantically relevant
7. Merge and deduplicate all three lists
8. Cap at 15-20 total, prioritize by: semantic relevance > importance > recency
9. Format into system prompt section
10. Return memories and formatted context

### 4.4 New CQRS: `TriggerMemoryExtraction.cs`

**Location**: `Codewrinkles.Application/Nova/TriggerMemoryExtraction.cs`

**Command**:
```csharp
public sealed record TriggerMemoryExtractionCommand(
    Guid ProfileId
) : ICommand<TriggerMemoryExtractionResult>;

public sealed record TriggerMemoryExtractionResult(
    int SessionsProcessed,
    int TotalMemoriesCreated
);
```

**Handler logic**:
1. Get all sessions needing memory extraction for this profile
2. For each session:
   - Create and execute `ExtractMemoriesCommand`
   - Aggregate results
3. Return total counts

**Note**: This could be fire-and-forget in the future, but start synchronous for simplicity.

### 4.5 Update: `SystemPrompts.cs`

**Add method**:
```csharp
/// <summary>
/// Builds the memory context section for the system prompt.
/// </summary>
public static string BuildMemoryContext(IReadOnlyList<MemoryDto> memories)
{
    if (memories.Count == 0)
        return string.Empty;

    var sb = new StringBuilder();
    sb.AppendLine();
    sb.AppendLine("## What I Remember From Our Past Conversations");

    // Group by category for cleaner presentation
    var topics = memories.Where(m => m.Category == "TopicDiscussed").ToList();
    var struggles = memories.Where(m => m.Category == "StruggleIdentified").ToList();
    var strengths = memories.Where(m => m.Category == "StrengthDemonstrated").ToList();
    var focus = memories.FirstOrDefault(m => m.Category == "CurrentFocus");

    if (focus != null)
        sb.AppendLine($"- Current focus: {focus.Content}");

    if (topics.Any())
        sb.AppendLine($"- Topics we've discussed: {string.Join(", ", topics.Select(t => t.Content))}");

    if (strengths.Any())
        sb.AppendLine($"- Your strengths: {string.Join(", ", strengths.Select(s => s.Content))}");

    if (struggles.Any())
        sb.AppendLine($"- Areas to reinforce: {string.Join(", ", struggles.Select(s => s.Content))}");

    return sb.ToString();
}
```

**Update `BuildPersonalizedPrompt`**:
```csharp
public static string BuildPersonalizedPrompt(LearnerProfile? profile, IReadOnlyList<MemoryDto>? memories = null)
{
    var prompt = new StringBuilder(CodyCoachBase);

    // Add learner profile section (existing code)
    if (profile is not null && profile.HasUserData())
    {
        // ... existing profile building code ...
    }

    // Add memory context section
    if (memories is not null && memories.Count > 0)
    {
        prompt.Append(BuildMemoryContext(memories));
    }

    return prompt.ToString();
}
```

### 4.6 Update: `SendMessage.cs`

**Update handler to integrate memories**:

```csharp
public async Task<SendMessageResult> HandleAsync(
    SendMessageCommand command,
    CancellationToken cancellationToken)
{
    // ... existing activity setup ...

    try
    {
        // Fetch learner profile for personalization (existing)
        var learnerProfile = await _unitOfWork.Nova.FindLearnerProfileByProfileIdAsync(
            command.ProfileId,
            cancellationToken);

        // NEW: Get memories for context
        IReadOnlyList<MemoryDto> memories = [];
        if (command.SessionId is null)
        {
            // New conversation - trigger extraction from previous sessions first
            // Then get relevant memories for this message
            await _mediator.SendAsync(
                new TriggerMemoryExtractionCommand(command.ProfileId),
                cancellationToken);
        }

        var memoriesResult = await _mediator.SendAsync(
            new GetMemoriesForContextQuery(command.ProfileId, command.Message),
            cancellationToken);
        memories = memoriesResult.Memories;

        // ... rest of existing handler ...

        // Update BuildLlmMessages call to include memories
        var llmMessages = BuildLlmMessages(history, command.Message, learnerProfile, memories);

        // ... rest of handler ...
    }
}

private static List<LlmMessage> BuildLlmMessages(
    IReadOnlyList<Message> history,
    string newMessage,
    LearnerProfile? learnerProfile,
    IReadOnlyList<MemoryDto> memories)
{
    // Build personalized system prompt with memories
    var systemPrompt = SystemPrompts.BuildPersonalizedPrompt(learnerProfile, memories);

    // ... rest of existing method ...
}
```

### 4.7 Update: `NovaEndpoints.cs` (Streaming)

Apply the same memory integration to the streaming endpoint.

### 4.8 Update: `SpanNames.cs`

**Add to Nova class**:
```csharp
public const string ExtractMemories = "Nova.ExtractMemories";
public const string GetMemoriesForContext = "Nova.GetMemoriesForContext";
public const string TriggerMemoryExtraction = "Nova.TriggerMemoryExtraction";
public const string GenerateEmbedding = "Nova.GenerateEmbedding";
```

---

## Part 5: Infrastructure - Embedding Service

### 5.1 New Service: `OpenAIEmbeddingService.cs`

**Location**: `Codewrinkles.Infrastructure/Services/OpenAIEmbeddingService.cs`

```csharp
using System.Buffers.Binary;
using Codewrinkles.Application.Common.Interfaces;
using Microsoft.SemanticKernel.Embeddings;

namespace Codewrinkles.Infrastructure.Services;

public sealed class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public OpenAIEmbeddingService(ITextEmbeddingGenerationService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(
            [text],
            cancellationToken: cancellationToken);

        return embeddings[0].ToArray();
    }

    public float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Embeddings must have same dimension");

        float dotProduct = 0;
        float normA = 0;
        float normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    public byte[] SerializeEmbedding(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        for (int i = 0; i < embedding.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(
                bytes.AsSpan(i * sizeof(float)),
                embedding[i]);
        }
        return bytes;
    }

    public float[] DeserializeEmbedding(byte[] data)
    {
        var embedding = new float[data.Length / sizeof(float)];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = BinaryPrimitives.ReadSingleLittleEndian(
                data.AsSpan(i * sizeof(float)));
        }
        return embedding;
    }
}
```

### 5.2 DI Registration

**Update `Program.cs` or service registration**:
```csharp
// Register embedding service (uses Semantic Kernel's embedding)
services.AddSingleton<IEmbeddingService, OpenAIEmbeddingService>();
```

---

## Part 6: API Layer Changes

### 6.1 Optional Debug Endpoints

**Add to `NovaEndpoints.cs`** (optional, for debugging):

```csharp
// Debug endpoints - consider removing in production
group.MapGet("memories", GetMemories)
    .WithName("NovaGetMemories");

group.MapDelete("memories/{memoryId:guid}", DeleteMemory)
    .WithName("NovaDeleteMemory");
```

---

## File Summary

### New Files (12)

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `Nova/MemoryCategory.cs` | Enum for memory types |
| Domain | `Nova/Memory.cs` | Memory entity |
| Infrastructure | `Configurations/Nova/MemoryConfiguration.cs` | EF Core config |
| Infrastructure | `Migrations/[timestamp]_AddMemorySystem.cs` | DB migration |
| Infrastructure | `Services/OpenAIEmbeddingService.cs` | Embedding generation |
| Infrastructure | `Repositories/Nova/NovaMemoryRepository.cs` | Memory repository impl |
| Application | `Common/Interfaces/IEmbeddingService.cs` | Embedding interface |
| Application | `Common/Interfaces/INovaMemoryRepository.cs` | Memory repository interface |
| Application | `Nova/ExtractMemories.cs` | Memory extraction handler |
| Application | `Nova/GetMemoriesForContext.cs` | Memory retrieval handler |
| Application | `Nova/TriggerMemoryExtraction.cs` | Extraction trigger handler |

### Modified Files (10)

| Layer | File | Changes |
|-------|------|---------|
| Domain | `Nova/ConversationSession.cs` | Add memory tracking fields |
| Infrastructure | `Configurations/Nova/ConversationSessionConfiguration.cs` | Configure new fields |
| Infrastructure | `Persistence/ApplicationDbContext.cs` | Add `Memories` DbSet |
| Infrastructure | `Persistence/Repositories/Nova/NovaRepository.cs` | Add session query methods |
| Infrastructure | `Persistence/UnitOfWork.cs` | Add `NovaMemories` property |
| Application | `Common/Interfaces/INovaRepository.cs` | Add session query methods |
| Application | `Common/Interfaces/IUnitOfWork.cs` | Add `NovaMemories` property |
| Application | `Nova/SystemPrompts.cs` | Add memory formatting |
| Application | `Nova/SendMessage.cs` | Integrate memory injection |
| Telemetry | `SpanNames.cs` | Add memory span names |
| API | `Modules/Nova/NovaEndpoints.cs` | Update streaming, optional debug endpoints |

---

## Implementation Order

1. **Domain entities** - Memory entity, MemoryCategory enum, update ConversationSession
2. **EF Core configurations** - MemoryConfiguration, update ConversationSessionConfiguration
3. **Migration** - Create and apply `AddMemorySystem`
4. **Repository interfaces** - INovaMemoryRepository, update INovaRepository, update IUnitOfWork
5. **Repository implementations** - NovaMemoryRepository, update NovaRepository, update UnitOfWork
6. **Embedding service** - IEmbeddingService interface, OpenAIEmbeddingService implementation
7. **CQRS handlers** - ExtractMemories, GetMemoriesForContext, TriggerMemoryExtraction
8. **System prompt integration** - Update SystemPrompts.BuildPersonalizedPrompt
9. **SendMessage integration** - Wire up memory extraction and injection
10. **Streaming endpoint integration** - Apply same changes to NovaEndpoints streaming
11. **Testing** - End-to-end flow verification

---

## Testing Checklist

- [ ] New conversation triggers extraction from previous sessions
- [ ] Returning to old conversation and adding messages works
- [ ] Starting another new conversation extracts those new messages
- [ ] Single-cardinality memories supersede correctly
- [ ] Multi-cardinality memories accumulate correctly
- [ ] Semantic search returns relevant memories
- [ ] System prompt includes memory context
- [ ] Token budget stays within limits
- [ ] Performance acceptable (extraction doesn't block chat)

---

## Future Enhancements (Not in this phase)

1. **Background job** - Daily consolidation of memories
2. **Memory decay** - Reduce importance over time
3. **Memory merging** - LLM-based consolidation of similar memories
4. **Vector database** - Move to pgvector/Pinecone for scale
5. **Memory UI** - Let users view/edit/delete their memories
