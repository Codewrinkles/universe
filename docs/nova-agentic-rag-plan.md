# Nova Agentic RAG: Technical Implementation Plan

> **Purpose**: Detailed week-by-week technical plan for implementing Agentic RAG with Semantic Kernel Plugins.
> **Last Updated**: December 22, 2025
> **Prerequisites**: CLAUDE.md standards, existing Nova implementation

---

## 1. Current State Analysis

### What We Have

| Component | Status | Location |
|-----------|--------|----------|
| Semantic Kernel | Installed | `SemanticKernelLlmService.cs` |
| Embedding Service | Working | `IEmbeddingService` + OpenAI text-embedding-3-small |
| Memory Entity | Complete | `Domain/Nova/Memory.cs` |
| Memory Repository | Complete | `INovaMemoryRepository` |
| LLM Service | Working | `ILlmService` (non-streaming + streaming) |
| SendMessage Flow | Working | Fetches profile + memories, builds prompt |

### Existing Patterns to Follow

From `CLAUDE.md`:

```csharp
// Entity pattern (from Memory.cs)
public sealed class EntityName
{
    // 1. Constants
    public const int MaxLength = 1000;

    // 2. EF Core constructor with pragma
    #pragma warning disable CS8618
    private EntityName() { }
    #pragma warning restore CS8618

    // 3. Properties
    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }  // Always DateTimeOffset

    // 4. Factory method
    public static EntityName Create(...) { }

    // 5. Public methods
    // 6. Private methods
}
```

```csharp
// Repository pattern (from ProfileRepository.cs)
public sealed class ProfileRepository : IProfileRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<Profile> _profiles;

    public ProfileRepository(ApplicationDbContext context)
    {
        _context = context;
        _profiles = context.Set<Profile>();
    }

    public async Task<Profile?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        return await _profiles.FindAsync([id], cancellationToken: ct);
    }

    // AsNoTracking for reads
    public async Task<IReadOnlyList<Profile>> GetAllAsync(CancellationToken ct)
    {
        return await _profiles.AsNoTracking().ToListAsync(ct);
    }

    // When you need another DbSet, access via _context.Set<T>()
    public async Task<IReadOnlyList<Profile>> GetMostFollowedAsync(int limit, CancellationToken ct)
    {
        var follows = _context.Set<Follow>();
        // ... join query
    }
}
```

### SK Already in Use

```csharp
// From SemanticKernelLlmService.cs - SK is already registered
private readonly IChatCompletionService _chatService;

public SemanticKernelLlmService(Kernel kernel, IOptions<NovaSettings> settings)
{
    _chatService = kernel.GetRequiredService<IChatCompletionService>();
}
```

---

## 2. Technical Analysis & Decision Points

### Vector Storage Options

Before implementing, we need to choose the right vector storage approach. Here's a comparison:

| Option | Pros | Cons | Cost |
|--------|------|------|------|
| **Azure AI Search** | Purpose-built for search, hybrid search (keyword + semantic), mature, scalable, semantic ranking | Additional service, learning curve, another dependency | Basic: ~$75/mo |
| **SQL Server 2025 (Native Vector)** | Integrated with existing stack, DiskANN algorithm, `VECTOR_SEARCH()` function, no separate service | Still in preview, table becomes READ-ONLY with vector index, requires single-column PK | Included with SQL Server |
| **Current Approach (varbinary)** | Simple, works now, no new services | Loads ALL embeddings into memory for similarity, doesn't scale, no ANN indexing | Free (existing infra) |

**Analysis:**

1. **Azure AI Search** ([docs](https://learn.microsoft.com/en-us/azure/search/vector-search-overview))
   - Best for production-grade search with hybrid capabilities
   - Supports multimodal (text + images)
   - ~$75/month for Basic tier (3 replicas, 3 partitions)
   - Recommended if: You need hybrid search, semantic ranking, or >100k documents

2. **SQL Server 2025 Native Vector** ([docs](https://learn.microsoft.com/en-us/sql/sql-server/ai/vectors))
   - New `VECTOR` data type and `VECTOR_SEARCH()` function
   - DiskANN algorithm for efficient ANN search
   - Limitation: Table with vector index becomes READ-ONLY during preview
   - Recommended if: You want to stay within SQL Server ecosystem, can wait for GA

3. **Current varbinary approach** (what Memory entity uses)
   - Fetches all embeddings, calculates similarity in memory
   - Works for <10k documents, degrades after that
   - Recommended if: Starting small, proving concept first

**Recommendation for Alpha/Beta:**
Start with **current approach** (proves concept, fast to implement), plan migration to **Azure AI Search** or **SQL Server 2025 native** when:
- Content exceeds 10k chunks
- Query latency becomes noticeable (>1s)
- Need hybrid search (keyword + semantic)

---

### Chunking Strategy

**Decision: Use SK TextChunker for ALL sources** (uniform approach for Alpha)

#### What SK TextChunker Provides

Based on [Microsoft Learn docs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.text.textchunker):

```csharp
// Available methods:
TextChunker.SplitPlainTextLines(text, maxTokensPerLine, tokenCounter)
TextChunker.SplitPlainTextParagraphs(lines, maxTokensPerParagraph, overlapTokens, tokenCounter)
TextChunker.SplitMarkDownLines(text, maxTokensPerLine, tokenCounter)
TextChunker.SplitMarkdownParagraphs(lines, maxTokensPerParagraph, overlapTokens, tokenCounter)
```

#### Unified Chunking Parameters (All Sources)

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Max tokens per chunk | 400 | Fits ~5 chunks in 2000 token budget |
| Overlap tokens | 50 | ~12% overlap, preserves context |
| Method | `SplitMarkdownParagraphs` for docs, `SplitPlainTextParagraphs` for others | Markdown-aware where applicable |

#### Pros of Uniform Approach

- ✅ Simpler implementation (one chunking path)
- ✅ No custom code to maintain
- ✅ SK is already installed
- ✅ Faster to ship Alpha
- ✅ Consistent chunk sizes across sources

#### Cons / Trade-offs

- ❌ No time-based chunking for YouTube (loses timestamp granularity)
- ❌ No semantic chunking (meaning-based grouping)
- ❌ No parent-child structure (could improve retrieval accuracy)
- ❌ May split code blocks mid-code
- ❌ Not optimized per source type

#### Future Improvement Path

Post-Alpha, can implement source-specific chunkers if retrieval quality is insufficient:
- YouTube: Custom time-based chunker with timestamp metadata
- Books: Structure-aware chunker using Azure Doc Intelligence's section detection
- Code: Syntax-aware chunker that preserves function/class boundaries

**References:**
- [SK TextChunker Docs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.text.textchunker)
- [Best Chunking Strategies 2025](https://www.firecrawl.dev/blog/best-chunking-strategies-rag-2025)
- [Azure RAG Chunking Guide](https://learn.microsoft.com/en-us/azure/architecture/ai-ml/guide/rag/rag-chunking-phase)

---

### Metadata Schema

Based on the analysis, here's the metadata we should capture:

| Field | Type | Purpose | Indexed? |
|-------|------|---------|----------|
| `Source` | enum | Filter by source type | Yes |
| `SourceIdentifier` | string | URL, video ID, book ISBN | Yes (unique with Source) |
| `Title` | string | Display in citations | No |
| `Author` | string | Filter by author (Evans, Fowler) | Yes |
| `Technology` | string | Filter by tech (dotnet, react) | Yes |
| `PublishedAt` | DateTimeOffset | Freshness ranking | Yes |
| `ParentDocumentId` | string | Group chunks from same source | Yes |
| `ChunkIndex` | int | Order within parent | No |
| `StartTime` | TimeSpan? | For video/audio sources | No |
| `EndTime` | TimeSpan? | For video/audio sources | No |
| `SectionPath` | string | Hierarchical location (Book > Chapter > Section) | No |
| `TokenCount` | int | For context window management | No |

---

### Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Vector Storage** | varbinary (current pattern) | Start simple, prove concept, migrate to Azure AI Search when >10k chunks |
| **Book Format** | PDF → Azure Document Intelligence | Quality extraction, handles complex layouts |
| **YouTube Ingestion** | Manual transcript upload | Full control, simpler pipeline |
| **Search Type** | Semantic only | Sufficient for coaching queries, simpler implementation |
| **Chunking** | Semantic Kernel text splitters | Already have SK installed, no new dependencies |
| **Token Budget** | 2000 tokens max | ~5 chunks per query, good balance |
| **Initial Scope** | 1 book + 1 video + 1 docs | Prove end-to-end before scaling |

---

### Admin UI: Content Ingestion

**Location**: `/admin/nova/content` (under existing Nova dropdown with Submissions and Metrics)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Nova > Content Ingestion                                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Upload PDF Book                                                      │   │
│  │ [Choose File]  Title: [________]  Author: [________]  [Upload]      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Upload YouTube Transcript                                            │   │
│  │ [Paste transcript or upload .txt]  Video URL: [________]  [Upload]  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Scrape Documentation Site                                            │   │
│  │ Homepage URL: [________________]  Technology: [____]  [Start Scrape]│   │
│  │ (Runs in background, crawls all pages from homepage)                │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Indexed Content                                        [Refresh]    │   │
│  │ ┌─────────────────────────────────────────────────────────────────┐│   │
│  │ │ Source          │ Title              │ Chunks │ Status │ Action ││   │
│  │ ├─────────────────┼────────────────────┼────────┼────────┼────────┤│   │
│  │ │ 📚 Book         │ DDD - Eric Evans   │ 45     │ ✅     │ [Del]  ││   │
│  │ │ 📺 YouTube      │ Clean Arch Video   │ 12     │ ✅     │ [Del]  ││   │
│  │ │ 📄 OfficialDocs │ .NET Fundamentals  │ 230    │ ⏳     │ [Del]  ││   │
│  │ └─────────────────────────────────────────────────────────────────┘│   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Files to create:**
- `apps/frontend/src/features/admin/ContentIngestionPage.tsx`
- Update `AdminNavigation.tsx` to add "Content" under Nova dropdown
- Update routing in `App.tsx`

---

### Background Processing: Channel Pattern

**Pattern**: Follow existing `EmailChannel` / `EmailSenderBackgroundService` pattern.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  How it works (from existing email implementation):                          │
│                                                                              │
│  1. API Endpoint receives upload request                                     │
│  2. Writes job to ContentIngestionChannel (fire-and-forget)                 │
│  3. Returns 202 Accepted immediately                                        │
│  4. ContentIngestionBackgroundService reads from channel                    │
│  5. Creates scope via IServiceScopeFactory (for scoped services)            │
│  6. Processes job (extract → chunk → embed → save)                          │
│  7. Updates job status in DB                                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Key pattern for scoped services (repositories):**

```csharp
// From EmailSenderBackgroundService.cs - this is how we access scoped services
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await foreach (var job in _channel.ReadAllAsync(stoppingToken))
    {
        try
        {
            // Create a scope per job - ensures proper lifetime for scoped services
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

            // Now we can use unitOfWork.ContentChunks, etc.
            await ProcessIngestionJobAsync(job, unitOfWork, embeddingService, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ingestion job {JobId}", job.Id);
            // Log and continue - never crash the service
        }
    }
}
```

**Files to create:**
- `ContentIngestionChannel.cs` - Singleton channel for job queue
- `ContentIngestionJob.cs` - Job record (PDF upload, transcript, scrape URL)
- `IContentIngestionQueue.cs` - Producer interface
- `ContentIngestionQueue.cs` - Implementation that writes to channel
- `ContentIngestionBackgroundService.cs` - Consumer that processes jobs

**DI Registration:**
```csharp
services.AddSingleton<ContentIngestionChannel>();
services.AddSingleton<IContentIngestionQueue, ContentIngestionQueue>();
services.AddHostedService<ContentIngestionBackgroundService>();
```

---

### Documentation Scraping

**Approach**: Crawl from homepage URL, extract all linked pages, chunk each.

```
User enters: https://learn.microsoft.com/en-us/dotnet/fundamentals/
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  ContentIngestionBackgroundService                                           │
│  1. Fetch homepage HTML                                                      │
│  2. Extract all internal links (same domain, under same path)               │
│  3. For each page:                                                           │
│     a. Fetch HTML                                                            │
│     b. Convert to Markdown (strip nav, footer, etc.)                        │
│     c. Chunk with SK TextChunker                                            │
│     d. Generate embeddings                                                   │
│     e. Save to ContentChunks                                                │
│  4. Rate limiting: 1 request/second to avoid being blocked                  │
│  5. Update job status as pages are processed                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Considerations:**
- Rate limiting (1 req/sec to avoid blocks)
- robots.txt respect
- Max pages limit (e.g., 500 pages per scrape job)
- Deduplication (don't re-scrape same URL)
- HTML → Markdown conversion (strip navigation, ads, etc.)

---

## 3. Architecture Overview

### Target Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              USER MESSAGE                                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          SendMessageCommandHandler                           │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ 1. Fetch LearnerProfile (existing)                                   │   │
│  │ 2. Fetch Memories (existing)                                         │   │
│  │ 3. Build base system prompt with profile + memories (existing)       │   │
│  │ 4. Call Semantic Kernel with plugins (NEW)                          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Semantic Kernel Orchestration                        │
│                                                                              │
│  Available Plugins:                                                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐            │
│  │ SearchContent   │  │ SearchOfficialDocs│  │  SearchPulse   │            │
│  │ Plugin          │  │ Plugin          │  │  Plugin         │            │
│  │                 │  │                 │  │                  │            │
│  │ search_books()  │  │ search_dotnet() │  │ search_pulses() │            │
│  │ search_youtube()│  │ search_react()  │  │                  │            │
│  │ search_articles()│ │ search_general()│  │                  │            │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘            │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Content Repository                                 │
│                                                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐ │
│  │ ContentChunk table with embeddings                                     │ │
│  │ - Source: Book, YouTube, OfficialDocs, Article, Pulse                  │ │
│  │ - Semantic search via IEmbeddingService                                │ │
│  └───────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Plugin Invocation Flow

```
1. User asks: "How should I structure aggregates in DDD?"

2. SK receives message + system prompt + plugin definitions

3. LLM decides to call: search_books("aggregates DDD", ["Eric Evans"])

4. Plugin executes:
   - Generates embedding for "aggregates DDD"
   - Searches ContentChunk where Source = Book
   - Returns top 5 matching chunks

5. LLM receives search results, formulates response

6. User sees grounded answer with authoritative information
```

---

## 4. Domain Layer

### New Entities

#### ContentChunk Entity

```csharp
// Location: apps/backend/src/Codewrinkles.Domain/Nova/ContentChunk.cs

namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Represents a chunk of content from an external source for RAG.
/// </summary>
public sealed class ContentChunk
{
    // Constants
    public const int MaxTitleLength = 500;
    public const int MaxContentLength = 8000;  // ~2000 tokens
    public const int MaxSourceIdentifierLength = 1000;

    // EF Core constructor
    #pragma warning disable CS8618
    private ContentChunk() { }
    #pragma warning restore CS8618

    // Properties
    public Guid Id { get; private set; }
    public ContentSource Source { get; private set; }
    public string SourceIdentifier { get; private set; }  // URL, video ID, book title
    public string Title { get; private set; }
    public string Content { get; private set; }
    public byte[] Embedding { get; private set; }
    public int TokenCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Optional metadata
    public string? Author { get; private set; }
    public string? Technology { get; private set; }  // "dotnet", "react", etc.
    public int? ChunkIndex { get; private set; }     // For multi-chunk documents
    public string? ParentDocumentId { get; private set; }  // Group chunks from same source
    public DateTimeOffset? PublishedAt { get; private set; }  // For freshness ranking
    public TimeSpan? StartTime { get; private set; }  // For video/audio sources
    public TimeSpan? EndTime { get; private set; }    // For video/audio sources
    public string? SectionPath { get; private set; }  // "DDD > Aggregates > Design Rules"

    // Factory method
    public static ContentChunk Create(
        ContentSource source,
        string sourceIdentifier,
        string title,
        string content,
        byte[] embedding,
        int tokenCount,
        string? author = null,
        string? technology = null,
        int? chunkIndex = null,
        string? parentDocumentId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(embedding);

        return new ContentChunk
        {
            Source = source,
            SourceIdentifier = sourceIdentifier.Trim()[..Math.Min(sourceIdentifier.Length, MaxSourceIdentifierLength)],
            Title = title.Trim()[..Math.Min(title.Length, MaxTitleLength)],
            Content = content.Trim()[..Math.Min(content.Length, MaxContentLength)],
            Embedding = embedding,
            TokenCount = tokenCount,
            Author = author?.Trim(),
            Technology = technology?.Trim().ToLowerInvariant(),
            ChunkIndex = chunkIndex,
            ParentDocumentId = parentDocumentId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // Public methods
    public void UpdateContent(string content, byte[] embedding, int tokenCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentNullException.ThrowIfNull(embedding);

        Content = content.Trim()[..Math.Min(content.Length, MaxContentLength)];
        Embedding = embedding;
        TokenCount = tokenCount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

#### ContentSource Enum

```csharp
// Location: apps/backend/src/Codewrinkles.Domain/Nova/ContentSource.cs

namespace Codewrinkles.Domain.Nova;

/// <summary>
/// The source type of a content chunk.
/// </summary>
public enum ContentSource
{
    /// <summary>
    /// Official documentation (Microsoft Learn, React docs, etc.)
    /// </summary>
    OfficialDocs = 0,

    /// <summary>
    /// Book summaries (Eric Evans DDD, Martin Fowler, etc.)
    /// </summary>
    Book = 1,

    /// <summary>
    /// YouTube video transcripts
    /// </summary>
    YouTube = 2,

    /// <summary>
    /// Blog articles from recognized experts
    /// </summary>
    Article = 3,

    /// <summary>
    /// Pulse posts from the community
    /// </summary>
    Pulse = 4
}
```

#### IngestionJobStatus Enum

```csharp
// Location: apps/backend/src/Codewrinkles.Domain/Nova/IngestionJobStatus.cs

namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Status of a content ingestion job.
/// </summary>
public enum IngestionJobStatus
{
    /// <summary>
    /// Job is queued and waiting to be processed.
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Job is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job failed with an error.
    /// </summary>
    Failed = 3
}
```

#### ContentIngestionJob Entity

```csharp
// Location: apps/backend/src/Codewrinkles.Domain/Nova/ContentIngestionJob.cs

namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Represents a background job for ingesting content (PDF, transcript, docs scrape).
/// </summary>
public sealed class ContentIngestionJob
{
    // Constants
    public const int MaxTitleLength = 500;
    public const int MaxUrlLength = 2000;
    public const int MaxErrorMessageLength = 4000;

    // EF Core constructor
    #pragma warning disable CS8618
    private ContentIngestionJob() { }
    #pragma warning restore CS8618

    // Properties
    public Guid Id { get; private set; }
    public ContentSource Source { get; private set; }
    public IngestionJobStatus Status { get; private set; }
    public string Title { get; private set; }
    public string ParentDocumentId { get; private set; }  // Used to group chunks

    // Source-specific metadata
    public string? Author { get; private set; }           // For books
    public string? Technology { get; private set; }       // For docs
    public string? SourceUrl { get; private set; }        // URL for docs/YouTube
    public int? MaxPages { get; private set; }            // For docs scraping

    // Progress tracking
    public int ChunksCreated { get; private set; }
    public int? TotalPages { get; private set; }
    public int? PagesProcessed { get; private set; }

    // Error handling
    public string? ErrorMessage { get; private set; }

    // Timestamps
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Factory methods
    public static ContentIngestionJob CreateForPdf(string fileName, string title, string author)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(author);

        return new ContentIngestionJob
        {
            Source = ContentSource.Book,
            Status = IngestionJobStatus.Queued,
            Title = title.Trim()[..Math.Min(title.Length, MaxTitleLength)],
            ParentDocumentId = $"book_{Guid.NewGuid():N}",
            Author = author.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ContentIngestionJob CreateForYouTube(string videoId, string videoUrl, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new ContentIngestionJob
        {
            Source = ContentSource.YouTube,
            Status = IngestionJobStatus.Queued,
            Title = title.Trim()[..Math.Min(title.Length, MaxTitleLength)],
            ParentDocumentId = $"yt_{videoId}",
            SourceUrl = videoUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ContentIngestionJob CreateForDocs(string homepageUrl, string technology, int maxPages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(homepageUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(technology);

        var uri = new Uri(homepageUrl);
        return new ContentIngestionJob
        {
            Source = ContentSource.OfficialDocs,
            Status = IngestionJobStatus.Queued,
            Title = $"{technology} Docs - {uri.Host}",
            ParentDocumentId = $"docs_{Guid.NewGuid():N}",
            SourceUrl = homepageUrl,
            Technology = technology.ToLowerInvariant(),
            MaxPages = maxPages,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // Public methods
    public void MarkAsProcessing()
    {
        Status = IngestionJobStatus.Processing;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateProgress(int pagesProcessed, int totalPages)
    {
        PagesProcessed = pagesProcessed;
        TotalPages = totalPages;
    }

    public void IncrementChunksCreated(int count = 1)
    {
        ChunksCreated += count;
    }

    public void MarkAsCompleted(int totalChunks)
    {
        Status = IngestionJobStatus.Completed;
        ChunksCreated = totalChunks;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = IngestionJobStatus.Failed;
        ErrorMessage = errorMessage[..Math.Min(errorMessage.Length, MaxErrorMessageLength)];
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
```

#### ContentIngestionJobConfiguration (EF Core)

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Nova/ContentIngestionJobConfiguration.cs

using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class ContentIngestionJobConfiguration : IEntityTypeConfiguration<ContentIngestionJob>
{
    public void Configure(EntityTypeBuilder<ContentIngestionJob> builder)
    {
        builder.ToTable("ContentIngestionJobs", "nova");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id)
            .ValueGeneratedOnAdd();

        builder.Property(j => j.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(ContentIngestionJob.MaxTitleLength);

        builder.Property(j => j.ParentDocumentId)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(j => j.Author)
            .HasMaxLength(200);

        builder.Property(j => j.Technology)
            .HasMaxLength(50);

        builder.Property(j => j.SourceUrl)
            .HasMaxLength(ContentIngestionJob.MaxUrlLength);

        builder.Property(j => j.ErrorMessage)
            .HasMaxLength(ContentIngestionJob.MaxErrorMessageLength);

        builder.Property(j => j.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        // Indexes
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.ParentDocumentId);
        builder.HasIndex(j => j.CreatedAt);
    }
}
```

#### IContentIngestionJobRepository Interface

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Common/Interfaces/IContentIngestionJobRepository.cs

using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IContentIngestionJobRepository
{
    /// <summary>
    /// Find job by ID.
    /// </summary>
    Task<ContentIngestionJob?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find job by parent document ID.
    /// </summary>
    Task<ContentIngestionJob?> FindByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all jobs with optional status filter.
    /// </summary>
    Task<IReadOnlyList<ContentIngestionJob>> GetAllAsync(
        IngestionJobStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get jobs that are queued and ready for processing.
    /// </summary>
    Task<IReadOnlyList<ContentIngestionJob>> GetQueuedJobsAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);

    void Create(ContentIngestionJob job);
    void Update(ContentIngestionJob job);
    void Delete(ContentIngestionJob job);
}
```

#### ContentIngestionJobRepository Implementation

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/ContentIngestionJobRepository.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class ContentIngestionJobRepository : IContentIngestionJobRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<ContentIngestionJob> _jobs;

    public ContentIngestionJobRepository(ApplicationDbContext context)
    {
        _context = context;
        _jobs = context.Set<ContentIngestionJob>();
    }

    public async Task<ContentIngestionJob?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _jobs.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<ContentIngestionJob?> FindByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default)
    {
        return await _jobs
            .FirstOrDefaultAsync(j => j.ParentDocumentId == parentDocumentId, cancellationToken);
    }

    public async Task<IReadOnlyList<ContentIngestionJob>> GetAllAsync(
        IngestionJobStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _jobs.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        return await query
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContentIngestionJob>> GetQueuedJobsAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await _jobs
            .AsNoTracking()
            .Where(j => j.Status == IngestionJobStatus.Queued)
            .OrderBy(j => j.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public void Create(ContentIngestionJob job)
    {
        _jobs.Add(job);
    }

    public void Update(ContentIngestionJob job)
    {
        _jobs.Update(job);
    }

    public void Delete(ContentIngestionJob job)
    {
        _jobs.Remove(job);
    }
}
```

---

## 5. Application Layer

### Repository Interface

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Common/Interfaces/IContentChunkRepository.cs

using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IContentChunkRepository
{
    // Query operations

    /// <summary>
    /// Find chunk by ID.
    /// </summary>
    Task<ContentChunk?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all chunks with embeddings for a specific source.
    /// </summary>
    Task<IReadOnlyList<ContentChunk>> GetBySourceAsync(
        ContentSource source,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all chunks with embeddings filtered by source and optional filters.
    /// Used for semantic search in application layer.
    /// </summary>
    Task<IReadOnlyList<ContentChunk>> GetWithEmbeddingsAsync(
        ContentSource? source = null,
        string? technology = null,
        string? author = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a chunk already exists for deduplication.
    /// </summary>
    Task<bool> ExistsBySourceIdentifierAsync(
        ContentSource source,
        string sourceIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chunk by source identifier for updates.
    /// </summary>
    Task<ContentChunk?> FindBySourceIdentifierAsync(
        ContentSource source,
        string sourceIdentifier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all chunks for a parent document (multi-chunk sources).
    /// </summary>
    Task<IReadOnlyList<ContentChunk>> GetByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default);

    // Write operations

    void Create(ContentChunk chunk);
    void Update(ContentChunk chunk);
    void Delete(ContentChunk chunk);

    /// <summary>
    /// Delete all chunks for a parent document (for re-ingestion).
    /// </summary>
    Task DeleteByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default);
}
```

### Add to IUnitOfWork

```csharp
// Update: apps/backend/src/Codewrinkles.Application/Common/Interfaces/IUnitOfWork.cs

public interface IUnitOfWork : IAsyncDisposable
{
    // ... existing repositories ...
    IContentChunkRepository ContentChunks { get; }  // NEW
}
```

### Content Search Service

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Nova/Services/IContentSearchService.cs

using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Nova.Services;

/// <summary>
/// Service for semantic search over content chunks.
/// </summary>
public interface IContentSearchService
{
    /// <summary>
    /// Search content by semantic similarity.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="source">Optional source filter.</param>
    /// <param name="technology">Optional technology filter.</param>
    /// <param name="author">Optional author filter.</param>
    /// <param name="limit">Maximum results to return.</param>
    /// <param name="minSimilarity">Minimum similarity threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ContentSearchResult>> SearchAsync(
        string query,
        ContentSource? source = null,
        string? technology = null,
        string? author = null,
        int limit = 5,
        float minSimilarity = 0.7f,
        CancellationToken cancellationToken = default);
}

public sealed record ContentSearchResult(
    Guid ChunkId,
    ContentSource Source,
    string Title,
    string Content,
    string? Author,
    string? Technology,
    float Similarity);
```

---

## 6. Infrastructure Layer

### EF Core Configuration

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Nova/ContentChunkConfiguration.cs

using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class ContentChunkConfiguration : IEntityTypeConfiguration<ContentChunk>
{
    public void Configure(EntityTypeBuilder<ContentChunk> builder)
    {
        builder.ToTable("ContentChunks", "nova");

        builder.HasKey(c => c.Id);

        // Sequential GUID generation (prevents index fragmentation)
        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.SourceIdentifier)
            .IsRequired()
            .HasMaxLength(ContentChunk.MaxSourceIdentifierLength);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(ContentChunk.MaxTitleLength);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(ContentChunk.MaxContentLength);

        builder.Property(c => c.Embedding)
            .IsRequired();

        builder.Property(c => c.TokenCount)
            .IsRequired();

        builder.Property(c => c.Author)
            .HasMaxLength(200);

        builder.Property(c => c.Technology)
            .HasMaxLength(50);

        builder.Property(c => c.ParentDocumentId)
            .HasMaxLength(500);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        // Indexes
        builder.HasIndex(c => c.Source);
        builder.HasIndex(c => c.Technology);
        builder.HasIndex(c => c.Author);
        builder.HasIndex(c => new { c.Source, c.SourceIdentifier }).IsUnique();
        builder.HasIndex(c => c.ParentDocumentId);
    }
}
```

### Repository Implementation

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/ContentChunkRepository.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories;

public sealed class ContentChunkRepository : IContentChunkRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<ContentChunk> _chunks;

    public ContentChunkRepository(ApplicationDbContext context)
    {
        _context = context;
        _chunks = context.Set<ContentChunk>();
    }

    public async Task<ContentChunk?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _chunks.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<ContentChunk>> GetBySourceAsync(
        ContentSource source,
        CancellationToken cancellationToken = default)
    {
        return await _chunks
            .AsNoTracking()
            .Where(c => c.Source == source)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContentChunk>> GetWithEmbeddingsAsync(
        ContentSource? source = null,
        string? technology = null,
        string? author = null,
        CancellationToken cancellationToken = default)
    {
        var query = _chunks.AsNoTracking();

        if (source.HasValue)
            query = query.Where(c => c.Source == source.Value);

        if (!string.IsNullOrWhiteSpace(technology))
            query = query.Where(c => c.Technology == technology.ToLowerInvariant());

        if (!string.IsNullOrWhiteSpace(author))
            query = query.Where(c => c.Author == author);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySourceIdentifierAsync(
        ContentSource source,
        string sourceIdentifier,
        CancellationToken cancellationToken = default)
    {
        return await _chunks
            .AsNoTracking()
            .AnyAsync(c => c.Source == source && c.SourceIdentifier == sourceIdentifier, cancellationToken);
    }

    public async Task<ContentChunk?> FindBySourceIdentifierAsync(
        ContentSource source,
        string sourceIdentifier,
        CancellationToken cancellationToken = default)
    {
        return await _chunks
            .FirstOrDefaultAsync(c => c.Source == source && c.SourceIdentifier == sourceIdentifier, cancellationToken);
    }

    public async Task<IReadOnlyList<ContentChunk>> GetByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default)
    {
        return await _chunks
            .AsNoTracking()
            .Where(c => c.ParentDocumentId == parentDocumentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);
    }

    public void Create(ContentChunk chunk)
    {
        _chunks.Add(chunk);
    }

    public void Update(ContentChunk chunk)
    {
        _chunks.Update(chunk);
    }

    public void Delete(ContentChunk chunk)
    {
        _chunks.Remove(chunk);
    }

    public async Task DeleteByParentDocumentIdAsync(
        string parentDocumentId,
        CancellationToken cancellationToken = default)
    {
        await _chunks
            .Where(c => c.ParentDocumentId == parentDocumentId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
```

### Content Search Service Implementation

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Services/Nova/ContentSearchService.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Application.Nova.Services;
using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Infrastructure.Services.Nova;

public sealed class ContentSearchService : IContentSearchService
{
    private readonly IContentChunkRepository _repository;
    private readonly IEmbeddingService _embeddingService;

    public ContentSearchService(
        IContentChunkRepository repository,
        IEmbeddingService embeddingService)
    {
        _repository = repository;
        _embeddingService = embeddingService;
    }

    public async Task<IReadOnlyList<ContentSearchResult>> SearchAsync(
        string query,
        ContentSource? source = null,
        string? technology = null,
        string? author = null,
        int limit = 5,
        float minSimilarity = 0.7f,
        CancellationToken cancellationToken = default)
    {
        // Generate embedding for query
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);

        // Get all matching chunks with embeddings
        var chunks = await _repository.GetWithEmbeddingsAsync(
            source,
            technology,
            author,
            cancellationToken);

        // Calculate similarity and filter
        var scored = new List<(ContentChunk Chunk, float Similarity)>();

        foreach (var chunk in chunks)
        {
            var chunkEmbedding = _embeddingService.DeserializeEmbedding(chunk.Embedding);
            var similarity = _embeddingService.CosineSimilarity(queryEmbedding, chunkEmbedding);

            if (similarity >= minSimilarity)
            {
                scored.Add((chunk, similarity));
            }
        }

        // Sort by similarity and take top N
        return scored
            .OrderByDescending(x => x.Similarity)
            .Take(limit)
            .Select(x => new ContentSearchResult(
                x.Chunk.Id,
                x.Chunk.Source,
                x.Chunk.Title,
                x.Chunk.Content,
                x.Chunk.Author,
                x.Chunk.Technology,
                x.Similarity))
            .ToList();
    }
}
```

### Background Processing: Channel Pattern Implementation

Following the existing `EmailChannel` / `EmailSenderBackgroundService` pattern from the codebase.

#### IContentIngestionQueue Interface

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Common/Interfaces/IContentIngestionQueue.cs

namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Producer interface for queuing content ingestion jobs.
/// </summary>
public interface IContentIngestionQueue
{
    /// <summary>
    /// Queue a PDF for background processing.
    /// </summary>
    Task QueuePdfIngestionAsync(
        Guid jobId,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a YouTube transcript for background processing.
    /// </summary>
    Task QueueTranscriptIngestionAsync(
        Guid jobId,
        string transcript,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a documentation scraping job for background processing.
    /// </summary>
    Task QueueDocsScrapeAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);
}
```

#### ContentIngestionChannel

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Services/Nova/ContentIngestionChannel.cs

using System.Threading.Channels;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Singleton channel for content ingestion jobs.
/// Fire-and-forget pattern: producers write to channel, background service consumes.
/// </summary>
public sealed class ContentIngestionChannel
{
    private readonly Channel<ContentIngestionMessage> _channel;

    public ContentIngestionChannel()
    {
        // Unbounded channel - jobs queue up without blocking
        // For production, consider BoundedChannelOptions with backpressure
        _channel = Channel.CreateUnbounded<ContentIngestionMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = true,  // Only one background service reads
                SingleWriter = false  // Multiple endpoints can queue
            });
    }

    public ChannelWriter<ContentIngestionMessage> Writer => _channel.Writer;
    public ChannelReader<ContentIngestionMessage> Reader => _channel.Reader;
}

/// <summary>
/// Message types for the ingestion channel.
/// </summary>
public abstract record ContentIngestionMessage(Guid JobId);

public sealed record PdfIngestionMessage(
    Guid JobId,
    byte[] PdfBytes) : ContentIngestionMessage(JobId);

public sealed record TranscriptIngestionMessage(
    Guid JobId,
    string Transcript) : ContentIngestionMessage(JobId);

public sealed record DocsScrapeMessage(
    Guid JobId) : ContentIngestionMessage(JobId);
```

#### ContentIngestionQueue Implementation

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Services/Nova/ContentIngestionQueue.cs

using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Producer implementation that writes to the ingestion channel.
/// </summary>
public sealed class ContentIngestionQueue : IContentIngestionQueue
{
    private readonly ContentIngestionChannel _channel;

    public ContentIngestionQueue(ContentIngestionChannel channel)
    {
        _channel = channel;
    }

    public async Task QueuePdfIngestionAsync(
        Guid jobId,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default)
    {
        var message = new PdfIngestionMessage(jobId, pdfBytes);
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public async Task QueueTranscriptIngestionAsync(
        Guid jobId,
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var message = new TranscriptIngestionMessage(jobId, transcript);
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public async Task QueueDocsScrapeAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var message = new DocsScrapeMessage(jobId);
        await _channel.Writer.WriteAsync(message, cancellationToken);
    }
}
```

#### ContentIngestionBackgroundService

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Services/Nova/ContentIngestionBackgroundService.cs

using System.Data;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Background service that consumes content ingestion jobs from the channel.
/// </summary>
public sealed class ContentIngestionBackgroundService : BackgroundService
{
    private readonly ContentIngestionChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContentIngestionBackgroundService> _logger;

    public ContentIngestionBackgroundService(
        ContentIngestionChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<ContentIngestionBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Content ingestion background service started");

        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Create a scope per job - ensures proper lifetime for scoped services
                using var scope = _scopeFactory.CreateScope();

                await ProcessMessageAsync(message, scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ingestion message for job {JobId}", message.JobId);
                // Log and continue - never crash the service
            }
        }
    }

    private async Task ProcessMessageAsync(
        ContentIngestionMessage message,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        switch (message)
        {
            case PdfIngestionMessage pdf:
                await ProcessPdfIngestionAsync(pdf.JobId, pdf.PdfBytes, services, cancellationToken);
                break;
            case TranscriptIngestionMessage transcript:
                await ProcessTranscriptIngestionAsync(transcript.JobId, transcript.Transcript, services, cancellationToken);
                break;
            case DocsScrapeMessage docs:
                await ProcessDocsScrapeAsync(docs.JobId, services, cancellationToken);
                break;
        }
    }

    private async Task ProcessPdfIngestionAsync(
        Guid jobId,
        byte[] pdfBytes,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var embeddingService = services.GetRequiredService<IEmbeddingService>();
        var pdfExtractor = services.GetRequiredService<IPdfExtractorService>();

        var job = await unitOfWork.ContentIngestionJobs.FindByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        job.MarkAsProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            // 1. Extract text from PDF (Azure Document Intelligence)
            var pages = await pdfExtractor.ExtractPagesAsync(pdfBytes, cancellationToken);
            job.UpdateProgress(0, pages.Count);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 2. Chunk and embed each page
            var chunkCount = 0;
            for (var i = 0; i < pages.Count; i++)
            {
                var pageContent = pages[i].Content;
                var pageChunks = TextChunker.SplitPlainTextParagraphs(
                    TextChunker.SplitPlainTextLines(pageContent, maxTokensPerLine: 100),
                    maxTokensPerParagraph: 400,
                    overlapTokens: 50);

                foreach (var chunkContent in pageChunks)
                {
                    if (string.IsNullOrWhiteSpace(chunkContent)) continue;

                    var embedding = await embeddingService.GetEmbeddingAsync(chunkContent, cancellationToken);
                    var embeddingBytes = embeddingService.SerializeEmbedding(embedding);
                    var tokenCount = EstimateTokens(chunkContent);

                    var chunk = ContentChunk.Create(
                        source: ContentSource.Book,
                        sourceIdentifier: $"{job.ParentDocumentId}_{chunkCount}",
                        title: $"{job.Title} - Page {i + 1}",
                        content: chunkContent,
                        embedding: embeddingBytes,
                        tokenCount: tokenCount,
                        author: job.Author,
                        parentDocumentId: job.ParentDocumentId,
                        chunkIndex: chunkCount);

                    unitOfWork.ContentChunks.Create(chunk);
                    chunkCount++;
                }

                // Update progress after each page
                job.UpdateProgress(i + 1, pages.Count);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 3. Mark job complete and commit
            job.MarkAsCompleted(chunkCount);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Completed PDF ingestion for job {JobId}: {ChunkCount} chunks created",
                jobId, chunkCount);
        }
        catch (Exception ex)
        {
            // Rollback all database changes
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Failed to process PDF for job {JobId}", jobId);

            // Update job status (separate operation, not in rolled-back transaction)
            job.MarkAsFailed(ex.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessTranscriptIngestionAsync(
        Guid jobId,
        string transcript,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var embeddingService = services.GetRequiredService<IEmbeddingService>();

        var job = await unitOfWork.ContentIngestionJobs.FindByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        job.MarkAsProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            // Chunk transcript using SK TextChunker
            var chunks = TextChunker.SplitPlainTextParagraphs(
                TextChunker.SplitPlainTextLines(transcript, maxTokensPerLine: 100),
                maxTokensPerParagraph: 400,
                overlapTokens: 50);

            var chunkCount = 0;
            foreach (var chunkContent in chunks)
            {
                if (string.IsNullOrWhiteSpace(chunkContent)) continue;

                var embedding = await embeddingService.GetEmbeddingAsync(chunkContent, cancellationToken);
                var embeddingBytes = embeddingService.SerializeEmbedding(embedding);
                var tokenCount = EstimateTokens(chunkContent);

                var chunk = ContentChunk.Create(
                    source: ContentSource.YouTube,
                    sourceIdentifier: $"{job.ParentDocumentId}_{chunkCount}",
                    title: $"{job.Title} (Part {chunkCount + 1})",
                    content: chunkContent,
                    embedding: embeddingBytes,
                    tokenCount: tokenCount,
                    parentDocumentId: job.ParentDocumentId,
                    chunkIndex: chunkCount);

                unitOfWork.ContentChunks.Create(chunk);
                chunkCount++;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            job.MarkAsCompleted(chunkCount);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Completed transcript ingestion for job {JobId}: {ChunkCount} chunks created",
                jobId, chunkCount);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Failed to process transcript for job {JobId}", jobId);

            job.MarkAsFailed(ex.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessDocsScrapeAsync(
        Guid jobId,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var embeddingService = services.GetRequiredService<IEmbeddingService>();
        var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

        var job = await unitOfWork.ContentIngestionJobs.FindByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found", jobId);
            return;
        }

        if (string.IsNullOrEmpty(job.SourceUrl))
        {
            job.MarkAsFailed("No source URL specified");
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        job.MarkAsProcessing();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        try
        {
            var httpClient = httpClientFactory.CreateClient("DocsScraper");
            var homepageUri = new Uri(job.SourceUrl);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var toVisit = new Queue<string>();
            toVisit.Enqueue(job.SourceUrl);

            var chunkCount = 0;
            var pagesProcessed = 0;
            var maxPages = job.MaxPages ?? 100;

            while (toVisit.Count > 0 && pagesProcessed < maxPages)
            {
                var url = toVisit.Dequeue();
                if (visited.Contains(url)) continue;
                visited.Add(url);

                try
                {
                    // Rate limiting: 1 request per second
                    await Task.Delay(1000, cancellationToken);

                    var html = await httpClient.GetStringAsync(url, cancellationToken);
                    var markdown = ConvertHtmlToMarkdown(html);

                    // Extract links for crawling
                    var links = ExtractInternalLinks(html, homepageUri);
                    foreach (var link in links)
                    {
                        if (!visited.Contains(link))
                        {
                            toVisit.Enqueue(link);
                        }
                    }

                    // Chunk the markdown content
                    var chunks = TextChunker.SplitMarkdownParagraphs(
                        TextChunker.SplitMarkDownLines(markdown, maxTokensPerLine: 100),
                        maxTokensPerParagraph: 400,
                        overlapTokens: 50);

                    foreach (var chunkContent in chunks)
                    {
                        if (string.IsNullOrWhiteSpace(chunkContent)) continue;

                        var embedding = await embeddingService.GetEmbeddingAsync(
                            chunkContent, cancellationToken);
                        var embeddingBytes = embeddingService.SerializeEmbedding(embedding);
                        var tokenCount = EstimateTokens(chunkContent);

                        var chunk = ContentChunk.Create(
                            source: ContentSource.OfficialDocs,
                            sourceIdentifier: $"{job.ParentDocumentId}_{chunkCount}",
                            title: ExtractPageTitle(html) ?? $"Page {pagesProcessed + 1}",
                            content: chunkContent,
                            embedding: embeddingBytes,
                            tokenCount: tokenCount,
                            technology: job.Technology,
                            parentDocumentId: job.ParentDocumentId,
                            chunkIndex: chunkCount);

                        unitOfWork.ContentChunks.Create(chunk);
                        chunkCount++;
                    }

                    pagesProcessed++;
                    job.UpdateProgress(pagesProcessed, Math.Min(visited.Count + toVisit.Count, maxPages));
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch {Url}", url);
                    // Continue with other pages
                }
            }

            job.MarkAsCompleted(chunkCount);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Completed docs scrape for job {JobId}: {ChunkCount} chunks from {PageCount} pages",
                jobId, chunkCount, pagesProcessed);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(ex, "Failed to scrape docs for job {JobId}", jobId);

            job.MarkAsFailed(ex.Message);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    // Helper methods
    private static int EstimateTokens(string text) => text.Length / 4;

    private static string ConvertHtmlToMarkdown(string html)
    {
        // Basic HTML to Markdown conversion
        // For production, use a library like ReverseMarkdown
        // Strip <script>, <style>, <nav>, <footer>, <header> tags first
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            html,
            @"<(script|style|nav|footer|header|aside)[^>]*>[\s\S]*?</\1>",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Strip remaining HTML tags
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<[^>]+>", " ");

        // Normalize whitespace
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

        return System.Net.WebUtility.HtmlDecode(cleaned.Trim());
    }

    private static IEnumerable<string> ExtractInternalLinks(string html, Uri baseUri)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(
            html,
            @"href=[""']([^""']+)[""']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var href = match.Groups[1].Value;
            if (Uri.TryCreate(baseUri, href, out var absoluteUri))
            {
                // Only internal links under the same path
                if (absoluteUri.Host == baseUri.Host &&
                    absoluteUri.AbsolutePath.StartsWith(baseUri.AbsolutePath))
                {
                    // Remove fragment and query
                    var cleanUrl = $"{absoluteUri.Scheme}://{absoluteUri.Host}{absoluteUri.AbsolutePath}";
                    yield return cleanUrl;
                }
            }
        }
    }

    private static string? ExtractPageTitle(string html)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            html,
            @"<title[^>]*>([^<]+)</title>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success ? System.Net.WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
    }
}
```

#### IPdfExtractorService Interface

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Common/Interfaces/IPdfExtractorService.cs

namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Service for extracting text from PDF documents.
/// </summary>
public interface IPdfExtractorService
{
    /// <summary>
    /// Extract text content from each page of a PDF.
    /// </summary>
    Task<IReadOnlyList<PdfPage>> ExtractPagesAsync(
        byte[] pdfBytes,
        CancellationToken cancellationToken = default);
}

public sealed record PdfPage(int PageNumber, string Content);
```

#### Azure Document Intelligence PDF Extractor

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Services/Nova/AzureDocumentIntelligencePdfExtractor.cs

using Azure;
using Azure.AI.DocumentIntelligence;
using Codewrinkles.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Codewrinkles.Infrastructure.Services.Nova;

public sealed class AzureDocumentIntelligencePdfExtractor : IPdfExtractorService
{
    private readonly DocumentIntelligenceClient _client;

    public AzureDocumentIntelligencePdfExtractor(IOptions<AzureDocumentIntelligenceSettings> settings)
    {
        var endpoint = new Uri(settings.Value.Endpoint);
        var credential = new AzureKeyCredential(settings.Value.ApiKey);
        _client = new DocumentIntelligenceClient(endpoint, credential);
    }

    public async Task<IReadOnlyList<PdfPage>> ExtractPagesAsync(
        byte[] pdfBytes,
        CancellationToken cancellationToken = default)
    {
        // Use prebuilt-read model for general document text extraction
        var content = BinaryData.FromBytes(pdfBytes);

        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-read",
            content,
            cancellationToken: cancellationToken);

        var result = operation.Value;
        var pages = new List<PdfPage>();

        if (result.Pages is not null)
        {
            foreach (var page in result.Pages)
            {
                var pageContent = ExtractPageContent(result, page.PageNumber);
                pages.Add(new PdfPage(page.PageNumber, pageContent));
            }
        }

        return pages;
    }

    private static string ExtractPageContent(AnalyzeResult result, int pageNumber)
    {
        // Collect all text from paragraphs on this page
        var lines = new List<string>();

        if (result.Paragraphs is not null)
        {
            foreach (var paragraph in result.Paragraphs)
            {
                // Check if paragraph is on this page
                if (paragraph.BoundingRegions?.Any(r => r.PageNumber == pageNumber) == true)
                {
                    lines.Add(paragraph.Content);
                }
            }
        }

        return string.Join("\n\n", lines);
    }
}

public sealed class AzureDocumentIntelligenceSettings
{
    public const string SectionName = "AzureDocumentIntelligence";

    public required string Endpoint { get; set; }
    public required string ApiKey { get; set; }
}
```

---

## 7. Semantic Kernel Plugins

### Base Plugin Setup

```csharp
// Location: apps/backend/src/Codewrinkles.Infrastructure/Services/Nova/Plugins/SearchContentPlugin.cs

using System.ComponentModel;
using System.Text;
using Codewrinkles.Application.Nova.Services;
using Codewrinkles.Domain.Nova;
using Microsoft.SemanticKernel;

namespace Codewrinkles.Infrastructure.Services.Nova.Plugins;

/// <summary>
/// Semantic Kernel plugin for searching content sources.
/// </summary>
public sealed class SearchContentPlugin
{
    private readonly IContentSearchService _searchService;

    public SearchContentPlugin(IContentSearchService searchService)
    {
        _searchService = searchService;
    }

    [KernelFunction("search_books")]
    [Description("Search book summaries for software architecture, design patterns, and methodology concepts. Use for questions about DDD, Clean Architecture, SOLID, design patterns, or methodologies.")]
    public async Task<string> SearchBooksAsync(
        [Description("The search query - be specific about the concept")] string query,
        [Description("Optional: Filter by author name (e.g., 'Eric Evans', 'Martin Fowler')")] string? author = null,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: ContentSource.Book,
            author: author,
            limit: 5,
            cancellationToken: cancellationToken);

        return FormatResults(results, "book summaries");
    }

    [KernelFunction("search_official_docs")]
    [Description("Search official documentation for specific API references, language features, and framework usage. Use for syntax questions, API details, or framework-specific guidance.")]
    public async Task<string> SearchOfficialDocsAsync(
        [Description("The search query - be specific about the API or feature")] string query,
        [Description("Technology to search: 'dotnet', 'csharp', 'react', 'typescript', 'efcore', 'aspnetcore'")] string technology,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: ContentSource.OfficialDocs,
            technology: technology.ToLowerInvariant(),
            limit: 5,
            cancellationToken: cancellationToken);

        return FormatResults(results, $"official {technology} documentation");
    }

    [KernelFunction("search_youtube")]
    [Description("Search Dan's YouTube video transcripts for practical tutorials, code walkthroughs, and real-world examples.")]
    public async Task<string> SearchYouTubeAsync(
        [Description("The search query")] string query,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: ContentSource.YouTube,
            limit: 5,
            cancellationToken: cancellationToken);

        return FormatResults(results, "YouTube tutorials");
    }

    [KernelFunction("search_articles")]
    [Description("Search expert blog articles for in-depth technical discussions and industry perspectives.")]
    public async Task<string> SearchArticlesAsync(
        [Description("The search query")] string query,
        [Description("Optional: Filter by author")] string? author = null,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: ContentSource.Article,
            author: author,
            limit: 5,
            cancellationToken: cancellationToken);

        return FormatResults(results, "expert articles");
    }

    [KernelFunction("search_pulse")]
    [Description("Search Pulse community posts for real-world experiences, discussions, and community insights.")]
    public async Task<string> SearchPulseAsync(
        [Description("The search query")] string query,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(
            query,
            source: ContentSource.Pulse,
            limit: 5,
            cancellationToken: cancellationToken);

        return FormatResults(results, "community discussions");
    }

    private static string FormatResults(IReadOnlyList<ContentSearchResult> results, string sourceDescription)
    {
        if (results.Count == 0)
        {
            return $"No relevant results found in {sourceDescription}.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Found {results.Count} relevant results from {sourceDescription}:");
        sb.AppendLine();

        foreach (var result in results)
        {
            sb.AppendLine($"### {result.Title}");
            if (!string.IsNullOrEmpty(result.Author))
                sb.AppendLine($"*Author: {result.Author}*");
            sb.AppendLine();
            sb.AppendLine(result.Content);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
```

---

## 8. Integrating Plugins into SendMessage

### Updated Service Interface

```csharp
// Update: apps/backend/src/Codewrinkles.Application/Common/Interfaces/ILlmService.cs

public interface ILlmService
{
    // ... existing methods ...

    /// <summary>
    /// Generates a chat completion response with function calling support.
    /// </summary>
    Task<LlmResponse> GetChatCompletionWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming chat completion with function calling support.
    /// </summary>
    IAsyncEnumerable<StreamingLlmChunk> GetStreamingChatCompletionWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default);
}
```

### Updated SemanticKernelLlmService

```csharp
// Key changes to SemanticKernelLlmService.cs

public sealed class SemanticKernelLlmService : ILlmService
{
    private readonly Kernel _kernel;  // Keep reference for plugin access
    private readonly IChatCompletionService _chatService;
    private readonly NovaSettings _settings;

    public SemanticKernelLlmService(Kernel kernel, IOptions<NovaSettings> settings)
    {
        _kernel = kernel;
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _settings = settings.Value;
    }

    public async Task<LlmResponse> GetChatCompletionWithToolsAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = BuildChatHistory(messages);
        var executionSettings = CreateExecutionSettingsWithTools();

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            _kernel,  // Pass kernel for function calling
            cancellationToken: cancellationToken);

        // ... token extraction ...

        return new LlmResponse(...);
    }

    private OpenAIPromptExecutionSettings CreateExecutionSettingsWithTools()
    {
        return new OpenAIPromptExecutionSettings
        {
            MaxTokens = _settings.MaxTokens,
            Temperature = _settings.Temperature,
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };
    }
}
```

### Plugin Registration (DI)

```csharp
// Location: apps/backend/src/Codewrinkles.API/Program.cs (or dedicated extension)

// In service registration:
services.AddSingleton<SearchContentPlugin>();

// When building Kernel:
var kernelBuilder = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(settings.ModelId, settings.OpenAIApiKey);

var kernel = kernelBuilder.Build();

// Import plugins
var searchPlugin = serviceProvider.GetRequiredService<SearchContentPlugin>();
kernel.ImportPluginFromObject(searchPlugin, "Search");
```

---

## 9. API Endpoints & CQRS Handlers

### Admin API Endpoints

Following the existing pattern in `AdminEndpoints.cs`, all content ingestion endpoints use the `AdminOnly` authorization policy.

```csharp
// Location: apps/backend/src/Codewrinkles.API/Modules/Admin/ContentEndpoints.cs

using Codewrinkles.Application.Nova;
using Codewrinkles.Domain.Nova;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Admin;

public static class ContentEndpoints
{
    public static void MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/content")
            .WithTags("Admin Content")
            .RequireAuthorization("AdminOnly");

        // Upload PDF for book ingestion
        group.MapPost("/pdf", UploadPdf)
            .WithName("UploadPdfContent")
            .DisableAntiforgery();  // For file uploads

        // Upload YouTube transcript
        group.MapPost("/transcript", UploadTranscript)
            .WithName("UploadTranscriptContent");

        // Start documentation scraping
        group.MapPost("/scrape", StartDocsScrape)
            .WithName("StartDocsScrape");

        // Get all ingestion jobs
        group.MapGet("/jobs", GetIngestionJobs)
            .WithName("GetIngestionJobs");

        // Get specific job status
        group.MapGet("/jobs/{id:guid}", GetIngestionJob)
            .WithName("GetIngestionJob");

        // Delete content by parent document ID
        group.MapDelete("/{parentDocumentId}", DeleteContent)
            .WithName("DeleteContent");
    }

    private static async Task<IResult> UploadPdf(
        [FromServices] IMediator mediator,
        [FromServices] IContentIngestionQueue queue,
        [FromForm] IFormFile file,
        [FromForm] string title,
        [FromForm] string author,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return Results.BadRequest(new { message = "No file uploaded" });
        }

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { message = "Only PDF files are accepted" });
        }

        // Create ingestion job
        var command = new CreatePdfIngestionJobCommand(
            FileName: file.FileName,
            Title: title,
            Author: author);
        var result = await mediator.SendAsync(command, cancellationToken);

        // Read file bytes and queue for background processing
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        var fileBytes = ms.ToArray();

        await queue.QueuePdfIngestionAsync(result.JobId, fileBytes, cancellationToken);

        return Results.Accepted(value: new
        {
            jobId = result.JobId,
            status = "queued",
            message = "PDF queued for processing"
        });
    }

    private static async Task<IResult> UploadTranscript(
        [FromServices] IMediator mediator,
        [FromServices] IContentIngestionQueue queue,
        [FromBody] UploadTranscriptRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Transcript))
        {
            return Results.BadRequest(new { message = "Transcript content is required" });
        }

        // Create ingestion job
        var command = new CreateTranscriptIngestionJobCommand(
            VideoId: request.VideoId,
            VideoUrl: request.VideoUrl,
            Title: request.Title);
        var result = await mediator.SendAsync(command, cancellationToken);

        // Queue for background processing
        await queue.QueueTranscriptIngestionAsync(
            result.JobId,
            request.Transcript,
            cancellationToken);

        return Results.Accepted(value: new
        {
            jobId = result.JobId,
            status = "queued",
            message = "Transcript queued for processing"
        });
    }

    private static async Task<IResult> StartDocsScrape(
        [FromServices] IMediator mediator,
        [FromServices] IContentIngestionQueue queue,
        [FromBody] StartDocsScrapeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.HomepageUrl))
        {
            return Results.BadRequest(new { message = "Homepage URL is required" });
        }

        if (!Uri.TryCreate(request.HomepageUrl, UriKind.Absolute, out var uri))
        {
            return Results.BadRequest(new { message = "Invalid URL format" });
        }

        // Create ingestion job
        var command = new CreateDocsScrapeJobCommand(
            HomepageUrl: request.HomepageUrl,
            Technology: request.Technology,
            MaxPages: request.MaxPages ?? 100);
        var result = await mediator.SendAsync(command, cancellationToken);

        // Queue for background processing
        await queue.QueueDocsScrapeAsync(result.JobId, cancellationToken);

        return Results.Accepted(value: new
        {
            jobId = result.JobId,
            status = "queued",
            message = "Documentation scraping started"
        });
    }

    private static async Task<IResult> GetIngestionJobs(
        [FromServices] IMediator mediator,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = new GetIngestionJobsQuery(status);
        var result = await mediator.QueryAsync(query, cancellationToken);

        return Results.Ok(new
        {
            jobs = result.Jobs.Select(j => new
            {
                id = j.Id,
                source = j.Source.ToString().ToLowerInvariant(),
                title = j.Title,
                status = j.Status.ToString().ToLowerInvariant(),
                chunksCreated = j.ChunksCreated,
                errorMessage = j.ErrorMessage,
                createdAt = j.CreatedAt,
                completedAt = j.CompletedAt
            })
        });
    }

    private static async Task<IResult> GetIngestionJob(
        [FromServices] IMediator mediator,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetIngestionJobQuery(id);
        var result = await mediator.QueryAsync(query, cancellationToken);

        if (result is null)
        {
            return Results.NotFound(new { message = "Job not found" });
        }

        return Results.Ok(new
        {
            id = result.Id,
            source = result.Source.ToString().ToLowerInvariant(),
            title = result.Title,
            status = result.Status.ToString().ToLowerInvariant(),
            parentDocumentId = result.ParentDocumentId,
            chunksCreated = result.ChunksCreated,
            totalPages = result.TotalPages,
            pagesProcessed = result.PagesProcessed,
            errorMessage = result.ErrorMessage,
            createdAt = result.CreatedAt,
            startedAt = result.StartedAt,
            completedAt = result.CompletedAt
        });
    }

    private static async Task<IResult> DeleteContent(
        [FromServices] IMediator mediator,
        [FromRoute] string parentDocumentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteContentCommand(parentDocumentId);
            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new
            {
                deletedChunks = result.DeletedChunks,
                message = $"Deleted {result.DeletedChunks} chunks"
            });
        }
        catch (ContentNotFoundException)
        {
            return Results.NotFound(new { message = "Content not found" });
        }
    }
}

// Request DTOs (received from frontend)
public sealed record UploadTranscriptRequest(
    string VideoId,
    string VideoUrl,
    string Title,
    string Transcript);

public sealed record StartDocsScrapeRequest(
    string HomepageUrl,
    string Technology,
    int? MaxPages);
```

### Register Endpoints

```csharp
// Update: apps/backend/src/Codewrinkles.API/Program.cs

// In endpoint mapping section:
app.MapContentEndpoints();
```

---

### CQRS Commands with Kommand

Following the existing pattern in `AcceptAlphaApplication.cs`:

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Nova/CreatePdfIngestionJob.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Command to create a PDF ingestion job
/// </summary>
public sealed record CreatePdfIngestionJobCommand(
    string FileName,
    string Title,
    string Author
) : ICommand<CreateIngestionJobResult>;

/// <summary>
/// Result with job ID for tracking
/// </summary>
public sealed record CreateIngestionJobResult(Guid JobId);

public sealed class CreatePdfIngestionJobCommandHandler
    : ICommandHandler<CreatePdfIngestionJobCommand, CreateIngestionJobResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreatePdfIngestionJobCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateIngestionJobResult> HandleAsync(
        CreatePdfIngestionJobCommand command,
        CancellationToken cancellationToken = default)
    {
        // Create job entity
        var job = ContentIngestionJob.CreateForPdf(
            fileName: command.FileName,
            title: command.Title,
            author: command.Author);

        _unitOfWork.ContentIngestionJobs.Create(job);

        // Single SaveChangesAsync - job creation is atomic
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateIngestionJobResult(job.Id);
    }
}
```

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Nova/CreateTranscriptIngestionJob.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Command to create a YouTube transcript ingestion job
/// </summary>
public sealed record CreateTranscriptIngestionJobCommand(
    string VideoId,
    string VideoUrl,
    string Title
) : ICommand<CreateIngestionJobResult>;

public sealed class CreateTranscriptIngestionJobCommandHandler
    : ICommandHandler<CreateTranscriptIngestionJobCommand, CreateIngestionJobResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateTranscriptIngestionJobCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateIngestionJobResult> HandleAsync(
        CreateTranscriptIngestionJobCommand command,
        CancellationToken cancellationToken = default)
    {
        var job = ContentIngestionJob.CreateForYouTube(
            videoId: command.VideoId,
            videoUrl: command.VideoUrl,
            title: command.Title);

        _unitOfWork.ContentIngestionJobs.Create(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateIngestionJobResult(job.Id);
    }
}
```

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Nova/CreateDocsScrapeJob.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Command to create a documentation scraping job
/// </summary>
public sealed record CreateDocsScrapeJobCommand(
    string HomepageUrl,
    string Technology,
    int MaxPages
) : ICommand<CreateIngestionJobResult>;

public sealed class CreateDocsScrapeJobCommandHandler
    : ICommandHandler<CreateDocsScrapeJobCommand, CreateIngestionJobResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateDocsScrapeJobCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateIngestionJobResult> HandleAsync(
        CreateDocsScrapeJobCommand command,
        CancellationToken cancellationToken = default)
    {
        var job = ContentIngestionJob.CreateForDocs(
            homepageUrl: command.HomepageUrl,
            technology: command.Technology,
            maxPages: command.MaxPages);

        _unitOfWork.ContentIngestionJobs.Create(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateIngestionJobResult(job.Id);
    }
}
```

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Nova/DeleteContent.cs

using Codewrinkles.Application.Common.Interfaces;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Command to delete all content chunks for a parent document
/// </summary>
public sealed record DeleteContentCommand(string ParentDocumentId) : ICommand<DeleteContentResult>;

public sealed record DeleteContentResult(int DeletedChunks);

public sealed class ContentNotFoundException : Exception
{
    public ContentNotFoundException(string parentDocumentId)
        : base($"Content with parent document ID '{parentDocumentId}' was not found")
    {
        ParentDocumentId = parentDocumentId;
    }

    public string ParentDocumentId { get; }
}

public sealed class DeleteContentCommandHandler
    : ICommandHandler<DeleteContentCommand, DeleteContentResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteContentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteContentResult> HandleAsync(
        DeleteContentCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check if content exists
        var chunks = await _unitOfWork.ContentChunks.GetByParentDocumentIdAsync(
            command.ParentDocumentId,
            cancellationToken);

        if (chunks.Count == 0)
        {
            throw new ContentNotFoundException(command.ParentDocumentId);
        }

        // Delete all chunks (uses ExecuteDeleteAsync - efficient bulk delete)
        await _unitOfWork.ContentChunks.DeleteByParentDocumentIdAsync(
            command.ParentDocumentId,
            cancellationToken);

        // Also delete the ingestion job if it exists
        var job = await _unitOfWork.ContentIngestionJobs.FindByParentDocumentIdAsync(
            command.ParentDocumentId,
            cancellationToken);

        if (job is not null)
        {
            _unitOfWork.ContentIngestionJobs.Delete(job);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new DeleteContentResult(chunks.Count);
    }
}
```

---

### CQRS Queries

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Nova/GetIngestionJobs.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Query to get all ingestion jobs with optional status filter
/// </summary>
public sealed record GetIngestionJobsQuery(string? Status) : IQuery<GetIngestionJobsResult>;

public sealed record GetIngestionJobsResult(IReadOnlyList<IngestionJobDto> Jobs);

public sealed record IngestionJobDto(
    Guid Id,
    ContentSource Source,
    string Title,
    IngestionJobStatus Status,
    int ChunksCreated,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed class GetIngestionJobsQueryHandler
    : IQueryHandler<GetIngestionJobsQuery, GetIngestionJobsResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetIngestionJobsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetIngestionJobsResult> HandleAsync(
        GetIngestionJobsQuery query,
        CancellationToken cancellationToken = default)
    {
        IngestionJobStatus? statusFilter = query.Status?.ToLowerInvariant() switch
        {
            "queued" => IngestionJobStatus.Queued,
            "processing" => IngestionJobStatus.Processing,
            "completed" => IngestionJobStatus.Completed,
            "failed" => IngestionJobStatus.Failed,
            _ => null
        };

        var jobs = await _unitOfWork.ContentIngestionJobs.GetAllAsync(
            statusFilter,
            cancellationToken);

        var dtos = jobs.Select(j => new IngestionJobDto(
            j.Id,
            j.Source,
            j.Title,
            j.Status,
            j.ChunksCreated,
            j.ErrorMessage,
            j.CreatedAt,
            j.CompletedAt
        )).ToList();

        return new GetIngestionJobsResult(dtos);
    }
}
```

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Nova/GetIngestionJob.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Query to get a specific ingestion job by ID
/// </summary>
public sealed record GetIngestionJobQuery(Guid JobId) : IQuery<IngestionJobDetailDto?>;

public sealed record IngestionJobDetailDto(
    Guid Id,
    ContentSource Source,
    string Title,
    IngestionJobStatus Status,
    string ParentDocumentId,
    int ChunksCreated,
    int? TotalPages,
    int? PagesProcessed,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);

public sealed class GetIngestionJobQueryHandler
    : IQueryHandler<GetIngestionJobQuery, IngestionJobDetailDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetIngestionJobQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IngestionJobDetailDto?> HandleAsync(
        GetIngestionJobQuery query,
        CancellationToken cancellationToken = default)
    {
        var job = await _unitOfWork.ContentIngestionJobs.FindByIdAsync(
            query.JobId,
            cancellationToken);

        if (job is null)
        {
            return null;
        }

        return new IngestionJobDetailDto(
            job.Id,
            job.Source,
            job.Title,
            job.Status,
            job.ParentDocumentId,
            job.ChunksCreated,
            job.TotalPages,
            job.PagesProcessed,
            job.ErrorMessage,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt);
    }
}
```

---

### Frontend/Backend DTO Contract

**Backend Response DTOs** (serialized as JSON from endpoints):

| Endpoint | Response Shape |
|----------|----------------|
| `POST /pdf` | `{ jobId: string, status: "queued", message: string }` |
| `POST /transcript` | `{ jobId: string, status: "queued", message: string }` |
| `POST /scrape` | `{ jobId: string, status: "queued", message: string }` |
| `GET /jobs` | `{ jobs: IngestionJobDto[] }` |
| `GET /jobs/{id}` | `IngestionJobDetailDto` |
| `DELETE /{parentDocumentId}` | `{ deletedChunks: number, message: string }` |

**Frontend TypeScript Types** (must match exactly):

```typescript
// Location: apps/frontend/src/features/admin/types.ts

export type ContentSource = 'officialdocs' | 'book' | 'youtube' | 'article' | 'pulse';
export type IngestionJobStatus = 'queued' | 'processing' | 'completed' | 'failed';

// Response from POST endpoints
export interface CreateJobResponse {
  jobId: string;
  status: 'queued';
  message: string;
}

// Response from GET /jobs
export interface IngestionJobsResponse {
  jobs: IngestionJob[];
}

export interface IngestionJob {
  id: string;
  source: ContentSource;
  title: string;
  status: IngestionJobStatus;
  chunksCreated: number;
  errorMessage: string | null;
  createdAt: string;
  completedAt: string | null;
}

// Response from GET /jobs/{id}
export interface IngestionJobDetail {
  id: string;
  source: ContentSource;
  title: string;
  status: IngestionJobStatus;
  parentDocumentId: string;
  chunksCreated: number;
  totalPages: number | null;
  pagesProcessed: number | null;
  errorMessage: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

// Response from DELETE
export interface DeleteContentResponse {
  deletedChunks: number;
  message: string;
}

// Request DTOs (sent to backend)
export interface UploadTranscriptRequest {
  videoId: string;
  videoUrl: string;
  title: string;
  transcript: string;
}

export interface StartDocsScrapeRequest {
  homepageUrl: string;
  technology: string;
  maxPages?: number;
}
```

---

### Transaction Handling

Following the pattern documented in CLAUDE.md (see `RegisterUser.cs`, `CreatePulse.cs`).

**When to Use Transactions:**

| Scenario | Use Transaction? | Reason |
|----------|------------------|--------|
| Create job (single entity) | ❌ No | `SaveChangesAsync` is already atomic |
| Create multiple related entities | ✅ Yes | All must exist or none (e.g., Job + initial metadata) |
| Background ingestion with external resources | ✅ Yes | Need rollback + external cleanup on failure |
| Delete content | ❌ No | `ExecuteDeleteAsync` is already atomic |

**Background Service Transaction Pattern:**

```csharp
// ContentIngestionBackgroundService.cs

private async Task ProcessPdfIngestionAsync(
    Guid jobId,
    byte[] pdfBytes,
    CancellationToken cancellationToken)
{
    using var scope = _scopeFactory.CreateScope();
    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    var job = await unitOfWork.ContentIngestionJobs.FindByIdAsync(jobId, cancellationToken);
    if (job is null) return;

    job.MarkAsProcessing();
    await unitOfWork.SaveChangesAsync(cancellationToken);

    // Track external resources for cleanup on failure
    var createdChunkIds = new List<Guid>();

    await using var transaction = await unitOfWork.BeginTransactionAsync(
        IsolationLevel.ReadCommitted,
        cancellationToken);
    try
    {
        // 1. Extract text from PDF (Azure Document Intelligence)
        var pages = await _pdfExtractor.ExtractPagesAsync(pdfBytes, cancellationToken);
        job.UpdateProgress(0, pages.Count);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Chunk and embed each page
        var chunkCount = 0;
        for (var i = 0; i < pages.Count; i++)
        {
            var pageChunks = ChunkPage(pages[i]);

            foreach (var chunk in pageChunks)
            {
                var embedding = await _embeddingService.GetEmbeddingAsync(
                    chunk.Content, cancellationToken);

                var entity = ContentChunk.Create(
                    source: ContentSource.Book,
                    sourceIdentifier: $"{job.ParentDocumentId}_{chunkCount}",
                    title: $"{job.Title} - {chunk.Section}",
                    content: chunk.Content,
                    embedding: _embeddingService.SerializeEmbedding(embedding),
                    tokenCount: chunk.TokenCount,
                    author: job.Author,
                    parentDocumentId: job.ParentDocumentId,
                    chunkIndex: chunkCount);

                unitOfWork.ContentChunks.Create(entity);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                createdChunkIds.Add(entity.Id);
                chunkCount++;
            }

            // Update progress after each page
            job.UpdateProgress(i + 1, pages.Count);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // 3. Mark job complete and commit
        job.MarkAsCompleted(chunkCount);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        // Rollback all database changes
        await transaction.RollbackAsync(cancellationToken);

        _logger.LogError(ex, "Failed to process PDF for job {JobId}", jobId);

        // Update job status (separate operation, not in rolled-back transaction)
        job.MarkAsFailed(ex.Message);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

**Key Points (from CLAUDE.md):**

1. ✅ **`await using var transaction`** - Ensures disposal even if exception occurs
2. ✅ **Multiple `SaveChangesAsync` calls within transaction** - Each writes to DB but not final until Commit
3. ✅ **Explicit `CommitAsync`** on success
4. ✅ **Explicit `RollbackAsync`** in catch block
5. ✅ **Mark job as failed AFTER rollback** (separate operation)
6. ✅ **Clean up external resources on rollback** (blobs, etc.)

**For Single-Entity Operations (No Transaction Needed):**

```csharp
// CreatePdfIngestionJob - single entity, no transaction
public async Task<CreateIngestionJobResult> HandleAsync(
    CreatePdfIngestionJobCommand command,
    CancellationToken cancellationToken = default)
{
    var job = ContentIngestionJob.CreateForPdf(
        fileName: command.FileName,
        title: command.Title,
        author: command.Author);

    _unitOfWork.ContentIngestionJobs.Create(job);
    await _unitOfWork.SaveChangesAsync(cancellationToken);  // Already atomic

    return new CreateIngestionJobResult(job.Id);
}
```

---

## 10. DI Registration & Configuration

### Complete Service Registration

```csharp
// Location: apps/backend/src/Codewrinkles.API/Program.cs (or dedicated extension method)

// === Content Ingestion Services ===

// Settings
builder.Services.Configure<AzureDocumentIntelligenceSettings>(
    builder.Configuration.GetSection(AzureDocumentIntelligenceSettings.SectionName));

// Singleton channel (shared state for fire-and-forget pattern)
builder.Services.AddSingleton<ContentIngestionChannel>();
builder.Services.AddSingleton<IContentIngestionQueue, ContentIngestionQueue>();

// Background service
builder.Services.AddHostedService<ContentIngestionBackgroundService>();

// Scoped services (created per-request/per-job)
builder.Services.AddScoped<IContentChunkRepository, ContentChunkRepository>();
builder.Services.AddScoped<IContentIngestionJobRepository, ContentIngestionJobRepository>();
builder.Services.AddScoped<IContentSearchService, ContentSearchService>();
builder.Services.AddScoped<IPdfExtractorService, AzureDocumentIntelligencePdfExtractor>();

// SK Plugin (singleton - stateless, uses injected services)
builder.Services.AddSingleton<SearchContentPlugin>(sp =>
{
    // Note: SearchContentPlugin needs IContentSearchService which is scoped
    // The plugin will be invoked within a scoped context (request pipeline)
    // so we create it with a factory that uses the scoped service provider
    return new SearchContentPlugin(
        sp.GetRequiredService<IContentSearchService>());
});

// HttpClient for docs scraping
builder.Services.AddHttpClient("DocsScraper", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "CodewrinklesBot/1.0 (+https://codewrinkles.com)");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### Update UnitOfWork

```csharp
// Update: apps/backend/src/Codewrinkles.Application/Common/Interfaces/IUnitOfWork.cs

public interface IUnitOfWork : IAsyncDisposable
{
    // ... existing repositories ...
    IContentChunkRepository ContentChunks { get; }
    IContentIngestionJobRepository ContentIngestionJobs { get; }
    // ... rest of interface
}
```

```csharp
// Update: apps/backend/src/Codewrinkles.Infrastructure/Persistence/UnitOfWork.cs

public sealed class UnitOfWork : IUnitOfWork
{
    // ... existing fields ...
    private readonly IContentChunkRepository _contentChunks;
    private readonly IContentIngestionJobRepository _contentIngestionJobs;

    public UnitOfWork(
        ApplicationDbContext context,
        // ... existing parameters ...
        IContentChunkRepository contentChunks,
        IContentIngestionJobRepository contentIngestionJobs)
    {
        // ... existing assignments ...
        _contentChunks = contentChunks;
        _contentIngestionJobs = contentIngestionJobs;
    }

    // ... existing properties ...
    public IContentChunkRepository ContentChunks => _contentChunks;
    public IContentIngestionJobRepository ContentIngestionJobs => _contentIngestionJobs;
}
```

### Update ApplicationDbContext

```csharp
// Update: apps/backend/src/Codewrinkles.Infrastructure/Persistence/ApplicationDbContext.cs

public class ApplicationDbContext : DbContext
{
    // ... existing DbSets ...
    public DbSet<ContentChunk> ContentChunks => Set<ContentChunk>();
    public DbSet<ContentIngestionJob> ContentIngestionJobs => Set<ContentIngestionJob>();
}
```

### Update Kernel Registration

```csharp
// Update existing Kernel registration to import plugins

// After building the kernel
var kernel = kernelBuilder.Build();

// Import the SearchContentPlugin
kernel.ImportPluginFromObject(
    sp.GetRequiredService<SearchContentPlugin>(),
    "Search");

// Register the configured kernel
services.AddSingleton(kernel);
```

### Configuration (appsettings.json)

```json
{
  "AzureDocumentIntelligence": {
    "Endpoint": "https://YOUR-RESOURCE.cognitiveservices.azure.com/",
    "ApiKey": "-- store in user secrets --"
  }
}
```

### User Secrets Setup

```bash
# Navigate to API project
cd apps/backend/src/Codewrinkles.API

# Set Azure Document Intelligence credentials
dotnet user-secrets set "AzureDocumentIntelligence:Endpoint" "https://YOUR-RESOURCE.cognitiveservices.azure.com/"
dotnet user-secrets set "AzureDocumentIntelligence:ApiKey" "your-api-key-here"
```

### NuGet Packages Required

```xml
<!-- Add to Codewrinkles.Infrastructure.csproj -->
<PackageReference Include="Azure.AI.DocumentIntelligence" Version="1.0.0" />
```

---

## 11. Content Ingestion (Legacy/Alternative Pattern)

### Ingestion Commands

```csharp
// Location: apps/backend/src/Codewrinkles.Application/Nova/IngestYouTubeContent.cs

public sealed record IngestYouTubeContentCommand(
    string VideoId,
    string Title,
    string Transcript
) : ICommand<IngestContentResult>;

public sealed record IngestContentResult(
    int ChunksCreated,
    int ChunksUpdated,
    int TokensTotal);

public sealed class IngestYouTubeContentCommandHandler
    : ICommandHandler<IngestYouTubeContentCommand, IngestContentResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITokenizer _tokenizer;  // For counting tokens

    public async Task<IngestContentResult> HandleAsync(
        IngestYouTubeContentCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Chunk transcript into meaningful segments
        var chunks = ChunkTranscript(command.Transcript, maxTokensPerChunk: 500);

        var created = 0;
        var updated = 0;
        var totalTokens = 0;

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunkContent = chunks[i];
            var tokenCount = _tokenizer.CountTokens(chunkContent);
            totalTokens += tokenCount;

            // Generate embedding
            var embedding = await _embeddingService.GetEmbeddingAsync(chunkContent, cancellationToken);
            var embeddingBytes = _embeddingService.SerializeEmbedding(embedding);

            var sourceId = $"{command.VideoId}_{i}";

            // Check if exists
            var existing = await _unitOfWork.ContentChunks.FindBySourceIdentifierAsync(
                ContentSource.YouTube,
                sourceId,
                cancellationToken);

            if (existing is not null)
            {
                existing.UpdateContent(chunkContent, embeddingBytes, tokenCount);
                _unitOfWork.ContentChunks.Update(existing);
                updated++;
            }
            else
            {
                var chunk = ContentChunk.Create(
                    source: ContentSource.YouTube,
                    sourceIdentifier: sourceId,
                    title: $"{command.Title} (Part {i + 1})",
                    content: chunkContent,
                    embedding: embeddingBytes,
                    tokenCount: tokenCount,
                    parentDocumentId: command.VideoId,
                    chunkIndex: i);

                _unitOfWork.ContentChunks.Create(chunk);
                created++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IngestContentResult(created, updated, totalTokens);
    }

    private static List<string> ChunkTranscript(string transcript, int maxTokensPerChunk)
    {
        // Simple paragraph-based chunking
        // Could be enhanced with semantic chunking later
        var paragraphs = transcript.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        var chunks = new List<string>();
        var currentChunk = new StringBuilder();
        var currentTokens = 0;

        foreach (var paragraph in paragraphs)
        {
            var paragraphTokens = EstimateTokens(paragraph);

            if (currentTokens + paragraphTokens > maxTokensPerChunk && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
                currentTokens = 0;
            }

            currentChunk.AppendLine(paragraph);
            currentTokens += paragraphTokens;
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    private static int EstimateTokens(string text)
    {
        // Rough estimate: ~4 chars per token for English
        return text.Length / 4;
    }
}
```

---

## 12. Week-by-Week Implementation Plan

### Week 1: Foundation + Domain + Background Processing ✅ COMPLETED

**Completed:** 2025-12-22 | **Commit:** `bccec5b`

| Day | Task | Details |
|-----|------|---------|
| **1** | Create domain entities | `ContentChunk.cs`, `ContentSource.cs`, `ContentIngestionJob.cs` |
| **1** | Add EF Core configuration | `ContentChunkConfiguration.cs`, `ContentIngestionJobConfiguration.cs` |
| **1** | Create migration | `dotnet ef migrations add AddContentChunks` |
| **2** | Repository interface | `IContentChunkRepository.cs`, `IContentIngestionJobRepository.cs` |
| **2** | Repository implementation | `ContentChunkRepository.cs`, `ContentIngestionJobRepository.cs` |
| **2** | Update UnitOfWork + DbContext | Add `ContentChunks`, `ContentIngestionJobs` properties |
| **3** | Channel pattern | `ContentIngestionChannel.cs`, `IContentIngestionQueue.cs`, `ContentIngestionQueue.cs` |
| **3** | Background service | `ContentIngestionBackgroundService.cs` (follows email pattern) |
| **4** | Content search service | `IContentSearchService.cs` + implementation (varbinary + in-memory similarity) |
| **4** | SK text splitter integration | Use `Microsoft.SemanticKernel.Text` for chunking |
| **5** | Azure Document Intelligence | Setup service for PDF extraction |
| **5** | Register services in DI | Update `Program.cs` |

**System Prompt Update (Week 1):** No changes yet - infrastructure only.

---

### Week 2: SK Plugin + Tool Calling

| Day | Task | Details |
|-----|------|---------|
| **1** | Create SearchContentPlugin | All 5 search functions with proper descriptions |
| **1** | Plugin DI registration | Import into Kernel |
| **2** | Update ILlmService | Add `GetChatCompletionWithToolsAsync` |
| **2** | Update SemanticKernelLlmService | Implement tool calling with `ToolCallBehavior.AutoInvokeKernelFunctions` |
| **3** | Update SendMessage handler | Use new method with plugins |
| **3** | System prompt update | Add tool usage instructions (see below) |
| **4** | Token budget enforcement | Limit RAG results to 2000 tokens |
| **4** | Handle streaming with tools | Ensure streaming works during tool execution |
| **5** | Unit tests | Test plugin invocation and search service |

**System Prompt Update (Week 2):** Add to `SystemPrompts.cs`:

```csharp
// Add after existing system prompt sections

private const string ToolUsageInstructions = """

    ## Available Knowledge Sources

    You have access to search tools for retrieving authoritative information.
    USE THESE TOOLS when answering technical questions to ensure accuracy.

    When to use each tool:

    - **search_books**: For architecture, design patterns, DDD, SOLID, methodologies
      Example queries: "aggregate design", "repository pattern", "bounded context"

    - **search_official_docs**: For API references, syntax, framework specifics
      Example: search_official_docs("dependency injection", "aspnetcore")

    - **search_youtube**: For practical tutorials, code examples, walkthroughs
      Good for "how to implement X" questions

    - **search_articles**: For industry perspectives, deep dives, expert opinions

    - **search_pulse**: For community experiences, real-world challenges

    ## Tool Usage Guidelines

    1. ALWAYS search before answering technical questions
    2. Cite your sources naturally: "According to Eric Evans..." or "The .NET docs recommend..."
    3. If search returns no results, acknowledge the gap and use your general knowledge
    4. Combine results from multiple sources when relevant
    5. Adapt retrieved content to the user's context and skill level
    """;
```

---

### Week 3: Admin UI + API Endpoints

| Day | Task | Details |
|-----|------|---------|
| **1** | API endpoints | `POST /api/admin/content/pdf`, `POST /api/admin/content/transcript`, `POST /api/admin/content/scrape` |
| **1** | API endpoints | `GET /api/admin/content/jobs`, `DELETE /api/admin/content/{id}` |
| **2** | ContentIngestionPage.tsx | Upload PDF form, upload transcript form, scrape URL form |
| **2** | Update AdminNavigation | Add "Content" under Nova dropdown |
| **3** | Indexed content list | Display jobs with status, chunk counts, delete action |
| **3** | Job status polling | Auto-refresh to show scraping progress |
| **4** | Docs scraping logic | HTML fetch → extract links → crawl → chunk |
| **4** | HTML to Markdown | Strip nav/footer, convert to clean markdown |
| **5** | Rate limiting + error handling | 1 req/sec, max pages limit, retry logic |

**System Prompt Update (Week 3):** No changes - admin UI only.

---

### Week 4: Initial Content + Testing

| Day | Task | Details |
|-----|------|---------|
| **1** | Ingest 1 YouTube video | Upload transcript via UI, verify chunking |
| **1** | Ingest 1 book chapter | Upload PDF via UI, verify Azure Doc Intelligence |
| **2** | Ingest 1 docs section | Scrape Microsoft Learn section via UI |
| **2** | Pulse integration | Query existing Pulse posts as content source |
| **3** | End-to-end testing | Ask questions that should hit each source |
| **3** | Verify tool calling | Confirm LLM calls correct search functions |
| **4** | Quality review | Verify citations, accuracy, response quality |
| **4** | Performance testing | Check latency with real content |
| **5** | Bug fixes + polish | Address any issues found in testing |
| **5** | Documentation | Update CLAUDE.md with new patterns if needed |

**System Prompt Update (Week 4):** Based on testing results, refine tool descriptions:

```csharp
// Optional refinements after testing (examples):

// If users ask about specific books, update search_books description:
[Description("Search book summaries including Eric Evans' Domain-Driven Design, Vaughn Vernon's Implementing DDD, and Martin Fowler's Patterns of Enterprise Application Architecture.")]

// If users need technology-specific guidance, update search_official_docs description:
[Description("Search official documentation for .NET, C#, ASP.NET Core, EF Core, React, and TypeScript. Use for API references and framework-specific guidance.")]

// Add any discovered edge cases or clarifications
```

---

## 13. Success Criteria

### Functional

- [ ] DDD questions cite Eric Evans/Vaughn Vernon concepts
- [ ] .NET questions align with Microsoft docs
- [ ] Tool calling works in streaming mode
- [ ] Ingestion pipeline handles 100+ videos
- [ ] Search returns relevant results (>70% precision)

### Performance

- [ ] Tool calling adds <2s latency
- [ ] Semantic search <500ms for 10k chunks
- [ ] Embedding generation <200ms per chunk

### Quality

- [ ] Alpha users notice improved accuracy
- [ ] Answers are consistent across sessions
- [ ] Sources are cited appropriately

---

## 14. Remaining Questions (To Decide During Implementation)

1. **Streaming UX**: Show "Searching..." indicator while tools execute?
2. **Fallback behavior**: If no relevant content found, use base LLM knowledge or say "I don't have information on that"?
3. **Citation format**: How to display sources in response? (inline, footnotes, expandable?)
4. **Which specific book**: Which Eric Evans/Fowler book to start with?
5. **Which specific video**: Which of your YouTube videos to ingest first?
6. **Which docs**: Microsoft Learn .NET basics, or something more specific?

---

## 15. Files to Create/Modify

### New Files

#### Domain Layer
| File | Location |
|------|----------|
| `ContentChunk.cs` | `Domain/Nova/` |
| `ContentSource.cs` | `Domain/Nova/` |
| `ContentIngestionJob.cs` | `Domain/Nova/` |
| `IngestionJobStatus.cs` | `Domain/Nova/` |

#### Application Layer
| File | Location |
|------|----------|
| `IContentChunkRepository.cs` | `Application/Common/Interfaces/` |
| `IContentIngestionJobRepository.cs` | `Application/Common/Interfaces/` |
| `IContentIngestionQueue.cs` | `Application/Common/Interfaces/` |
| `IPdfExtractorService.cs` | `Application/Common/Interfaces/` |
| `IContentSearchService.cs` | `Application/Nova/Services/` |
| `CreatePdfIngestionJob.cs` | `Application/Nova/` |
| `CreateTranscriptIngestionJob.cs` | `Application/Nova/` |
| `CreateDocsScrapeJob.cs` | `Application/Nova/` |
| `DeleteContent.cs` | `Application/Nova/` |
| `GetIngestionJobs.cs` | `Application/Nova/` |
| `GetIngestionJob.cs` | `Application/Nova/` |

#### Infrastructure Layer
| File | Location |
|------|----------|
| `ContentChunkRepository.cs` | `Infrastructure/Persistence/Repositories/` |
| `ContentIngestionJobRepository.cs` | `Infrastructure/Persistence/Repositories/` |
| `ContentChunkConfiguration.cs` | `Infrastructure/Persistence/Configurations/Nova/` |
| `ContentIngestionJobConfiguration.cs` | `Infrastructure/Persistence/Configurations/Nova/` |
| `ContentSearchService.cs` | `Infrastructure/Services/Nova/` |
| `SearchContentPlugin.cs` | `Infrastructure/Services/Nova/Plugins/` |
| `ContentIngestionChannel.cs` | `Infrastructure/Services/Nova/` |
| `ContentIngestionQueue.cs` | `Infrastructure/Services/Nova/` |
| `ContentIngestionBackgroundService.cs` | `Infrastructure/Services/Nova/` |
| `AzureDocumentIntelligencePdfExtractor.cs` | `Infrastructure/Services/Nova/` |
| EF Migration | `Infrastructure/Persistence/Migrations/` |

#### API Layer
| File | Location |
|------|----------|
| `ContentEndpoints.cs` | `API/Modules/Admin/` |

#### Frontend
| File | Location |
|------|----------|
| `ContentIngestionPage.tsx` | `features/admin/` |
| `types.ts` (update) | `features/admin/` |

### Modified Files

| File | Changes |
|------|---------|
| `IUnitOfWork.cs` | Add `IContentChunkRepository ContentChunks`, `IContentIngestionJobRepository ContentIngestionJobs` |
| `UnitOfWork.cs` | Implement `ContentChunks`, `ContentIngestionJobs` properties |
| `ApplicationDbContext.cs` | Add `DbSet<ContentChunk>`, `DbSet<ContentIngestionJob>` |
| `ILlmService.cs` | Add tool-enabled methods |
| `SemanticKernelLlmService.cs` | Implement tool calling with `ToolCallBehavior.AutoInvokeKernelFunctions` |
| `SendMessage.cs` | Use tool-enabled LLM method |
| `SystemPrompts.cs` | Add tool usage instructions |
| `Program.cs` | Register new services, import plugins, map content endpoints |
| `AdminNavigation.tsx` | Add "Content" under Nova dropdown |
| `App.tsx` | Add route for `/admin/nova/content` |

---

**Next Step**: Start with Week 1, Day 1 - create `ContentChunk` entity and `ContentSource` enum.
