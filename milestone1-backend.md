# Milestone 1 Backend Implementation Plan

> **Version**: 1.0
> **Last Updated**: 2025-12-14
> **Status**: Planning Complete - Ready for Implementation

---

## Executive Summary

This document provides a comprehensive implementation plan for Nova's backend (Milestone 1: Nova Can Chat). The plan strictly follows all existing architecture patterns established in the Codewrinkles codebase, including:

- Clean Architecture with feature-based folder organization
- DateTimeOffset for all timestamps (never DateTime)
- Sequential GUIDs via EF Core's ValueGeneratedOnAdd
- CQRS with Kommand library
- Unit of Work pattern with scoped repositories
- Minimal API modules with endpoint grouping
- OpenTelemetry instrumentation

**Technology Decision**: Use **Microsoft Semantic Kernel** (v1.68.0) for OpenAI integration. This is the stable, production-ready framework (vs. Agent Framework which is in preview). Semantic Kernel provides:
- Native OpenAI connector with streaming support
- Clean abstractions for chat completion
- Memory management for conversation history
- Future extensibility for RAG (Milestone 2+)

---

## 1. Folder Structure

Following the existing feature-based organization pattern used throughout the codebase. The Application layer uses **flat feature folders** (no nested Commands/Queries/DTOs subfolders).

```
apps/backend/src/
├── Codewrinkles.Domain/Nova/
│   ├── ConversationSession.cs              # Main conversation entity
│   ├── Message.cs                          # Individual chat message
│   ├── MessageRole.cs                      # Enum: User, Assistant, System
│   └── Exceptions/
│       ├── ConversationNotFoundException.cs
│       └── ConversationAccessDeniedException.cs
│
├── Codewrinkles.Application/
│   ├── Nova/                               # Feature folder (flat structure like Pulse/)
│   │   ├── SendMessage.cs                  # Command + Handler + Result (all in one file)
│   │   ├── SendMessageValidator.cs         # Validator in separate file
│   │   ├── GetConversation.cs              # Query + Handler + Result
│   │   ├── GetConversations.cs             # Query + Handler + Result
│   │   ├── DeleteConversation.cs           # Command + Handler + Result
│   │   ├── DeleteConversationValidator.cs  # Validator
│   │   ├── NovaDtos.cs                     # All DTOs in one file (like PulseDtos.cs)
│   │   └── SystemPrompts.cs                # Cody's coaching personality
│   │
│   └── Common/
│       └── Interfaces/
│           ├── INovaRepository.cs          # NEW: Nova repository interface
│           ├── ILlmService.cs              # NEW: LLM abstraction
│           └── (existing interfaces...)
│
├── Codewrinkles.Infrastructure/
│   ├── Persistence/
│   │   ├── Repositories/
│   │   │   ├── Nova/                       # NEW: Nova subfolder for repositories
│   │   │   │   └── NovaRepository.cs
│   │   │   ├── PulseRepository.cs          # (existing - stays at root)
│   │   │   ├── IdentityRepository.cs       # (existing)
│   │   │   └── ...
│   │   │
│   │   └── Configurations/
│   │       ├── Nova/                       # Existing pattern
│   │       │   ├── ConversationSessionConfiguration.cs
│   │       │   └── MessageConfiguration.cs
│   │       ├── Pulse/                      # (existing)
│   │       └── ...
│   │
│   └── Services/
│       └── Nova/                           # Existing pattern
│           ├── SemanticKernelLlmService.cs
│           └── NovaSettings.cs
│
└── Codewrinkles.API/Modules/Nova/
    └── NovaEndpoints.cs                    # Minimal API endpoints
```

**Key Pattern Notes:**
- Application layer feature folders are **flat** (no Commands/, Queries/, DTOs/ subfolders)
- All DTOs go in a single file: `NovaDtos.cs` (following `PulseDtos.cs` pattern)
- Command/Query + Handler + Result are in **one file** (following `CreatePulse.cs` pattern)
- Validators are in **separate files** (following `CreatePulseValidator.cs` pattern)
- Nova exceptions go in `Domain/Nova/Exceptions/` (following `Domain/Pulse/Exceptions/` pattern)
- Repository interfaces go in `Common/Interfaces/` (following `IPulseRepository.cs` pattern)
- Infrastructure repositories get a `Nova/` subfolder for future growth

---

## 2. Domain Layer

### 2.1 ConversationSession Entity

Following the exact pattern from `Pulse.cs`:

```csharp
// File: Codewrinkles.Domain/Nova/ConversationSession.cs

using Codewrinkles.Domain.Identity;

namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Represents a Nova conversation session between a user and the AI coach.
/// </summary>
public sealed class ConversationSession
{
    // Constants
    public const int MaxTitleLength = 200;

    // Private parameterless constructor for EF Core materialization only
    // EF Core will populate all properties via reflection when loading from database
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private ConversationSession() { }
#pragma warning restore CS8618

    // Properties
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public string? Title { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastMessageAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation properties
    public Profile Owner { get; private set; }
    public ICollection<Message> Messages { get; private set; }

    // Factory methods
    public static ConversationSession Create(Guid profileId, string? title = null)
    {
        var trimmedTitle = title?.Trim();

        if (trimmedTitle is not null && trimmedTitle.Length > MaxTitleLength)
        {
            throw new ArgumentException(
                $"Conversation title cannot exceed {MaxTitleLength} characters.",
                nameof(title));
        }

        return new ConversationSession
        {
            // Id will be generated by EF Core using sequential GUID generation
            ProfileId = profileId,
            Title = trimmedTitle,
            CreatedAt = DateTimeOffset.UtcNow,
            LastMessageAt = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Messages = new List<Message>()
        };
    }

    // Public methods
    public void UpdateTitle(string title)
    {
        var trimmedTitle = title.Trim();

        if (trimmedTitle.Length > MaxTitleLength)
        {
            throw new ArgumentException(
                $"Conversation title cannot exceed {MaxTitleLength} characters.",
                nameof(title));
        }

        Title = trimmedTitle;
    }

    public void UpdateLastMessageAt()
    {
        LastMessageAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }
}
```

### 2.2 Message Entity

```csharp
// File: Codewrinkles.Domain/Nova/Message.cs

namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Represents a single message in a Nova conversation.
/// </summary>
public sealed class Message
{
    // Constants
    public const int MaxContentLength = 100_000; // ~25k tokens worth of text

    // Private parameterless constructor for EF Core materialization only
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private Message() { }
#pragma warning restore CS8618

    // Properties
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public int? TokensUsed { get; private set; }
    public string? ModelUsed { get; private set; }

    // Navigation properties
    public ConversationSession Session { get; private set; }

    // Factory methods
    public static Message CreateUserMessage(Guid sessionId, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        var trimmedContent = content.Trim();

        if (trimmedContent.Length > MaxContentLength)
        {
            throw new ArgumentException(
                $"Message content cannot exceed {MaxContentLength} characters.",
                nameof(content));
        }

        return new Message
        {
            // Id will be generated by EF Core using sequential GUID generation
            SessionId = sessionId,
            Role = MessageRole.User,
            Content = trimmedContent,
            CreatedAt = DateTimeOffset.UtcNow,
            TokensUsed = null,
            ModelUsed = null
        };
    }

    public static Message CreateAssistantMessage(
        Guid sessionId,
        string content,
        int? tokensUsed = null,
        string? modelUsed = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        return new Message
        {
            // Id will be generated by EF Core using sequential GUID generation
            SessionId = sessionId,
            Role = MessageRole.Assistant,
            Content = content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            TokensUsed = tokensUsed,
            ModelUsed = modelUsed
        };
    }

    public static Message CreateSystemMessage(Guid sessionId, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        return new Message
        {
            // Id will be generated by EF Core using sequential GUID generation
            SessionId = sessionId,
            Role = MessageRole.System,
            Content = content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            TokensUsed = null,
            ModelUsed = null
        };
    }
}
```

### 2.3 MessageRole Enum

```csharp
// File: Codewrinkles.Domain/Nova/MessageRole.cs

namespace Codewrinkles.Domain.Nova;

/// <summary>
/// The role of a message in a conversation.
/// </summary>
public enum MessageRole : byte
{
    /// <summary>
    /// Message from the user.
    /// </summary>
    User = 0,

    /// <summary>
    /// Message from the AI assistant (Cody).
    /// </summary>
    Assistant = 1,

    /// <summary>
    /// System message (e.g., coaching personality instructions).
    /// </summary>
    System = 2
}
```

### 2.4 Domain Exceptions

```csharp
// File: Codewrinkles.Domain/Nova/Exceptions/ConversationNotFoundException.cs

namespace Codewrinkles.Domain.Nova.Exceptions;

public sealed class ConversationNotFoundException : Exception
{
    public Guid ConversationId { get; }

    public ConversationNotFoundException(Guid conversationId)
        : base($"Conversation with ID '{conversationId}' was not found.")
    {
        ConversationId = conversationId;
    }
}
```

```csharp
// File: Codewrinkles.Domain/Nova/Exceptions/ConversationAccessDeniedException.cs

namespace Codewrinkles.Domain.Nova.Exceptions;

public sealed class ConversationAccessDeniedException : Exception
{
    public Guid ConversationId { get; }
    public Guid ProfileId { get; }

    public ConversationAccessDeniedException(Guid conversationId, Guid profileId)
        : base($"Profile '{profileId}' does not have access to conversation '{conversationId}'.")
    {
        ConversationId = conversationId;
        ProfileId = profileId;
    }
}
```

---

## 3. Infrastructure Layer - EF Core Configurations

### 3.1 ConversationSessionConfiguration

Following the exact pattern from `PulseConfiguration.cs`:

```csharp
// File: Codewrinkles.Infrastructure/Persistence/Configurations/Nova/ConversationSessionConfiguration.cs

using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class ConversationSessionConfiguration : IEntityTypeConfiguration<ConversationSession>
{
    public void Configure(EntityTypeBuilder<ConversationSession> builder)
    {
        // Table configuration - nova schema
        builder.ToTable("ConversationSessions", "nova");

        // Primary key
        builder.HasKey(c => c.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        // This avoids index fragmentation issues with random GUIDs
        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(c => c.Title)
            .HasMaxLength(ConversationSession.MaxTitleLength)
            .IsRequired(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.LastMessageAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(c => c.Owner)
            .WithMany()
            .HasForeignKey(c => c.ProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        // Composite index for listing user's conversations
        builder.HasIndex(c => new { c.ProfileId, c.IsDeleted, c.LastMessageAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("IX_ConversationSessions_ProfileId_IsDeleted_LastMessageAt");

        // Single-column indexes for specific queries
        builder.HasIndex(c => c.ProfileId)
            .HasDatabaseName("IX_ConversationSessions_ProfileId");

        builder.HasIndex(c => c.IsDeleted)
            .HasDatabaseName("IX_ConversationSessions_IsDeleted");
    }
}
```

### 3.2 MessageConfiguration

```csharp
// File: Codewrinkles.Infrastructure/Persistence/Configurations/Nova/MessageConfiguration.cs

using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Nova;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        // Table configuration - nova schema
        builder.ToTable("Messages", "nova");

        // Primary key
        builder.HasKey(m => m.Id);

        // Primary key value generation - EF Core will use sequential GUID generation
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(m => m.Role)
            .HasConversion<byte>()
            .IsRequired();

        builder.Property(m => m.Content)
            .HasMaxLength(Message.MaxContentLength)
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(m => m.TokensUsed)
            .IsRequired(false);

        builder.Property(m => m.ModelUsed)
            .HasMaxLength(100)
            .IsRequired(false);

        // Indexes
        // Composite index for fetching conversation messages in order
        builder.HasIndex(m => new { m.SessionId, m.CreatedAt })
            .HasDatabaseName("IX_Messages_SessionId_CreatedAt");
    }
}
```

### 3.3 ApplicationDbContext Update

Add Nova DbSets to the existing ApplicationDbContext:

```csharp
// In ApplicationDbContext.cs - add these DbSets

public DbSet<ConversationSession> ConversationSessions => Set<ConversationSession>();
public DbSet<Message> NovaMessages => Set<Message>();
```

---

## 4. Infrastructure Layer - Repository

### 4.1 INovaRepository Interface

```csharp
// File: Codewrinkles.Application/Common/Interfaces/INovaRepository.cs

using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

public interface INovaRepository
{
    // ConversationSession operations
    Task<ConversationSession?> FindSessionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ConversationSession?> FindSessionByIdWithMessagesAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationSession>> GetSessionsByProfileIdAsync(
        Guid profileId,
        int limit,
        DateTimeOffset? beforeLastMessageAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default);

    void CreateSession(ConversationSession session);

    void UpdateSession(ConversationSession session);

    // Message operations
    Task<Message?> FindMessageByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Message>> GetMessagesBySessionIdAsync(
        Guid sessionId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    void CreateMessage(Message message);

    Task<int> GetMessageCountBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
```

### 4.2 NovaRepository Implementation

Following the pattern from `PulseRepository.cs`:

```csharp
// File: Codewrinkles.Infrastructure/Persistence/Repositories/Nova/NovaRepository.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

namespace Codewrinkles.Infrastructure.Persistence.Repositories.Nova;

public sealed class NovaRepository : INovaRepository
{
    private readonly ApplicationDbContext _context;
    private readonly DbSet<ConversationSession> _sessions;
    private readonly DbSet<Message> _messages;

    public NovaRepository(ApplicationDbContext context)
    {
        _context = context;
        _sessions = context.Set<ConversationSession>();
        _messages = context.Set<Message>();
    }

    public async Task<ConversationSession?> FindSessionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _sessions.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<ConversationSession?> FindSessionByIdWithMessagesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _sessions
            .AsNoTracking()
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSession>> GetSessionsByProfileIdAsync(
        Guid profileId,
        int limit,
        DateTimeOffset? beforeLastMessageAt,
        Guid? beforeId,
        CancellationToken cancellationToken = default)
    {
        var query = _sessions
            .AsNoTracking()
            .Where(s => s.ProfileId == profileId && !s.IsDeleted);

        // Cursor-based pagination
        if (beforeLastMessageAt.HasValue && beforeId.HasValue)
        {
            query = query.Where(s =>
                s.LastMessageAt < beforeLastMessageAt.Value ||
                (s.LastMessageAt == beforeLastMessageAt.Value && s.Id.CompareTo(beforeId.Value) < 0));
        }

        query = query
            .OrderByDescending(s => s.LastMessageAt)
            .ThenByDescending(s => s.Id)
            .Take(limit);

        return await query.ToListAsync(cancellationToken);
    }

    public void CreateSession(ConversationSession session)
    {
        _sessions.Add(session);
    }

    public void UpdateSession(ConversationSession session)
    {
        _sessions.Update(session);
    }

    public async Task<Message?> FindMessageByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _messages.FindAsync([id], cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Message>> GetMessagesBySessionIdAsync(
        Guid sessionId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _messages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt);

        if (limit.HasValue)
        {
            // Get the most recent N messages
            query = (IOrderedQueryable<Message>)_messages
                .AsNoTracking()
                .Where(m => m.SessionId == sessionId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit.Value)
                .OrderBy(m => m.CreatedAt);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public void CreateMessage(Message message)
    {
        _messages.Add(message);
    }

    public async Task<int> GetMessageCountBySessionIdAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _messages
            .Where(m => m.SessionId == sessionId)
            .CountAsync(cancellationToken);
    }
}
```

### 4.3 IUnitOfWork Update

Add Nova repository to the existing IUnitOfWork interface:

```csharp
// In IUnitOfWork.cs - add this property

INovaRepository Nova { get; }
```

And in the UnitOfWork implementation:

```csharp
// In UnitOfWork.cs - add

public INovaRepository Nova { get; }

// In constructor
Nova = new NovaRepository(context);
```

---

## 5. Infrastructure Layer - LLM Service

### 5.1 ILlmService Interface

```csharp
// File: Codewrinkles.Application/Common/Interfaces/ILlmService.cs

using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Abstraction for LLM chat completion services.
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Generates a chat completion response.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assistant's response with metadata.</returns>
    Task<LlmResponse> GetChatCompletionAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming chat completion response.
    /// </summary>
    /// <param name="messages">The conversation history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of response chunks.</returns>
    IAsyncEnumerable<LlmResponseChunk> GetStreamingChatCompletionAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a message for the LLM.
/// </summary>
public sealed record LlmMessage(MessageRole Role, string Content);

/// <summary>
/// Represents a complete LLM response.
/// </summary>
public sealed record LlmResponse(
    string Content,
    int InputTokens,
    int OutputTokens,
    string ModelUsed);

/// <summary>
/// Represents a streaming response chunk.
/// </summary>
public sealed record LlmResponseChunk(
    string Content,
    bool IsComplete,
    int? InputTokens = null,
    int? OutputTokens = null,
    string? ModelUsed = null);
```

### 5.2 NovaSettings Configuration

```csharp
// File: Codewrinkles.Infrastructure/Services/Nova/NovaSettings.cs

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Configuration settings for Nova AI service.
/// </summary>
public sealed class NovaSettings
{
    public const string SectionName = "Nova";

    /// <summary>
    /// OpenAI API key. Store in User Secrets, not appsettings.json.
    /// </summary>
    public string OpenAIApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The OpenAI model to use for chat completions.
    /// Default: gpt-4o-mini for cost efficiency.
    /// </summary>
    public string ModelId { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Maximum tokens in the response.
    /// </summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Temperature for response randomness (0.0 - 2.0).
    /// Lower = more deterministic, higher = more creative.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum number of messages to include in conversation context.
    /// Prevents token overflow for long conversations.
    /// </summary>
    public int MaxContextMessages { get; set; } = 20;
}
```

### 5.3 SemanticKernelLlmService Implementation

```csharp
// File: Codewrinkles.Infrastructure/Services/Nova/SemanticKernelLlmService.cs

using System.Runtime.CompilerServices;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// LLM service implementation using Microsoft Semantic Kernel with OpenAI.
/// </summary>
public sealed class SemanticKernelLlmService : ILlmService
{
    private readonly IChatCompletionService _chatService;
    private readonly NovaSettings _settings;

    public SemanticKernelLlmService(
        Kernel kernel,
        IOptions<NovaSettings> settings)
    {
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        _settings = settings.Value;
    }

    public async Task<LlmResponse> GetChatCompletionAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = BuildChatHistory(messages);
        var executionSettings = CreateExecutionSettings();

        var response = await _chatService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            cancellationToken: cancellationToken);

        // Extract token usage from metadata
        var inputTokens = 0;
        var outputTokens = 0;

        if (response.Metadata?.TryGetValue("Usage", out var usageObj) == true &&
            usageObj is OpenAI.Chat.ChatTokenUsage usage)
        {
            inputTokens = usage.InputTokenCount;
            outputTokens = usage.OutputTokenCount;
        }

        return new LlmResponse(
            Content: response.Content ?? string.Empty,
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            ModelUsed: _settings.ModelId);
    }

    public async IAsyncEnumerable<LlmResponseChunk> GetStreamingChatCompletionAsync(
        IReadOnlyList<LlmMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistory = BuildChatHistory(messages);
        var executionSettings = CreateExecutionSettings();

        var streamingResponse = _chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings,
            cancellationToken: cancellationToken);

        await foreach (var chunk in streamingResponse.WithCancellation(cancellationToken))
        {
            if (chunk.Content is not null)
            {
                // Check if this is the final chunk with metadata
                int? inputTokens = null;
                int? outputTokens = null;

                if (chunk.Metadata?.TryGetValue("Usage", out var usageObj) == true &&
                    usageObj is OpenAI.Chat.ChatTokenUsage usage)
                {
                    inputTokens = usage.InputTokenCount;
                    outputTokens = usage.OutputTokenCount;
                }

                yield return new LlmResponseChunk(
                    Content: chunk.Content,
                    IsComplete: false,
                    InputTokens: inputTokens,
                    OutputTokens: outputTokens,
                    ModelUsed: inputTokens.HasValue ? _settings.ModelId : null);
            }
        }

        // Signal completion
        yield return new LlmResponseChunk(
            Content: string.Empty,
            IsComplete: true,
            ModelUsed: _settings.ModelId);
    }

    private ChatHistory BuildChatHistory(IReadOnlyList<LlmMessage> messages)
    {
        var chatHistory = new ChatHistory();

        foreach (var message in messages)
        {
            var role = message.Role switch
            {
                MessageRole.User => AuthorRole.User,
                MessageRole.Assistant => AuthorRole.Assistant,
                MessageRole.System => AuthorRole.System,
                _ => throw new ArgumentOutOfRangeException(nameof(message.Role))
            };

            chatHistory.Add(new ChatMessageContent(role, message.Content));
        }

        return chatHistory;
    }

    private OpenAIPromptExecutionSettings CreateExecutionSettings()
    {
        return new OpenAIPromptExecutionSettings
        {
            ModelId = _settings.ModelId,
            MaxTokens = _settings.MaxTokens,
            Temperature = _settings.Temperature
        };
    }
}
```

### 5.4 Dependency Injection Registration

```csharp
// In Program.cs or a DI extension method

// Configure Nova settings
builder.Services.Configure<NovaSettings>(
    builder.Configuration.GetSection(NovaSettings.SectionName));

// Register Semantic Kernel
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<NovaSettings>>().Value;

    var kernelBuilder = Kernel.CreateBuilder();
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: settings.ModelId,
        apiKey: settings.OpenAIApiKey);

    return kernelBuilder.Build();
});

// Register LLM service
builder.Services.AddScoped<ILlmService, SemanticKernelLlmService>();
```

---

## 6. Application Layer - System Prompts

### 6.1 SystemPrompts Class

```csharp
// File: Codewrinkles.Application/Nova/SystemPrompts.cs

namespace Codewrinkles.Application.Nova;

/// <summary>
/// System prompts for Nova's coaching personality (Cody).
/// </summary>
public static class SystemPrompts
{
    /// <summary>
    /// The main system prompt that establishes Cody's coaching personality.
    /// </summary>
    public const string CoachingPersonality = """
        You are Cody, an AI learning coach for software development and architecture,
        created by Codewrinkles. You help developers learn and grow in their technical skills.

        ## Your Personality
        - Friendly, approachable, and encouraging
        - Patient with beginners, challenging for experts
        - Honest about complexity - never oversimplify when it matters
        - Enthusiastic about software craftsmanship
        - Practical and pragmatic, not dogmatic

        ## Your Role
        - Help developers learn and grow in their technical skills
        - Provide clear, accurate explanations of complex concepts
        - Guide users through architectural decisions with trade-off analysis
        - Share best practices from the software development community
        - Adapt your explanations to the user's apparent experience level

        ## Guidelines
        - Start with a direct answer, then elaborate if needed
        - Use code examples when they clarify concepts (format properly with language tags)
        - When multiple valid approaches exist, present them fairly with trade-offs
        - Acknowledge when you're uncertain or when a topic is genuinely debatable
        - Break complex topics into digestible pieces
        - Ask clarifying questions when the user's question is ambiguous

        ## Topics You Help With
        - Software architecture (Clean Architecture, DDD, Microservices, etc.)
        - Design patterns and SOLID principles
        - .NET/C#, TypeScript/JavaScript, and other languages
        - Database design and optimization
        - API design and best practices
        - DevOps, CI/CD, and deployment
        - Code review and refactoring
        - Career growth and learning paths
        - Any software development topic

        ## Response Format
        - Keep responses focused and concise unless the topic warrants depth
        - Use markdown formatting for code blocks, lists, and emphasis
        - For code examples, always specify the language
        - End with a follow-up question or suggestion when appropriate

        ## What NOT to Do
        - Don't be preachy or lecture unnecessarily
        - Don't repeat disclaimers about being an AI
        - Don't use corporate jargon or buzzwords without substance
        - Don't refuse to help with legitimate technical questions
        - Don't provide generic advice - be specific and actionable
        """;

    /// <summary>
    /// Generates a title for a conversation based on the first user message.
    /// </summary>
    public const string TitleGeneration = """
        Based on this user message, generate a short title (3-6 words) that summarizes the topic.
        Return ONLY the title, no explanation or quotes.

        User message: {0}
        """;
}
```

---

## 7. Application Layer - Commands & Queries

### 7.1 SendMessage Command (Core Chat Flow)

Following the pattern from `CreatePulse.cs`:

```csharp
// File: Codewrinkles.Application/Nova/SendMessage.cs

using System.Data;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Domain.Nova.Exceptions;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record SendMessageCommand(
    Guid ProfileId,
    Guid? SessionId,
    string Content
) : ICommand<SendMessageResult>;

public sealed record SendMessageResult(
    Guid SessionId,
    Guid MessageId,
    string AssistantResponse,
    DateTimeOffset CreatedAt,
    bool IsNewSession,
    string? SessionTitle
);

public sealed class SendMessageCommandHandler
    : ICommandHandler<SendMessageCommand, SendMessageResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILlmService _llmService;
    private readonly NovaSettings _settings;

    public SendMessageCommandHandler(
        IUnitOfWork unitOfWork,
        ILlmService llmService,
        IOptions<NovaSettings> settings)
    {
        _unitOfWork = unitOfWork;
        _llmService = llmService;
        _settings = settings.Value;
    }

    public async Task<SendMessageResult> HandleAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.SendMessage);
        activity?.SetProfileId(command.ProfileId);

        try
        {
            ConversationSession session;
            var isNewSession = false;

            await using var transaction = await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            try
            {
                // Get or create conversation session
                if (command.SessionId.HasValue)
                {
                    session = await _unitOfWork.Nova.FindSessionByIdAsync(
                        command.SessionId.Value,
                        cancellationToken)
                        ?? throw new ConversationNotFoundException(command.SessionId.Value);

                    // Verify ownership
                    if (session.ProfileId != command.ProfileId)
                    {
                        throw new ConversationAccessDeniedException(
                            command.SessionId.Value,
                            command.ProfileId);
                    }

                    if (session.IsDeleted)
                    {
                        throw new ConversationNotFoundException(command.SessionId.Value);
                    }
                }
                else
                {
                    // Create new conversation
                    session = ConversationSession.Create(command.ProfileId);
                    _unitOfWork.Nova.CreateSession(session);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    isNewSession = true;
                }

                activity?.SetEntity("ConversationSession", session.Id);

                // Create user message
                var userMessage = Message.CreateUserMessage(session.Id, command.Content);
                _unitOfWork.Nova.CreateMessage(userMessage);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Get conversation history for context
                var existingMessages = await _unitOfWork.Nova.GetMessagesBySessionIdAsync(
                    session.Id,
                    _settings.MaxContextMessages,
                    cancellationToken);

                // Build LLM messages (system prompt + conversation history)
                var llmMessages = BuildLlmMessages(existingMessages);

                // Get LLM response
                var llmResponse = await _llmService.GetChatCompletionAsync(
                    llmMessages,
                    cancellationToken);

                // Create assistant message
                var assistantMessage = Message.CreateAssistantMessage(
                    session.Id,
                    llmResponse.Content,
                    llmResponse.InputTokens + llmResponse.OutputTokens,
                    llmResponse.ModelUsed);
                _unitOfWork.Nova.CreateMessage(assistantMessage);

                // Update session
                session.UpdateLastMessageAt();

                // Generate title for new sessions
                if (isNewSession && string.IsNullOrEmpty(session.Title))
                {
                    var title = await GenerateTitleAsync(command.Content, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        session.UpdateTitle(title);
                    }
                }

                _unitOfWork.Nova.UpdateSession(session);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                // Record metrics
                AppMetrics.RecordNovaMessage(
                    isNewSession: isNewSession,
                    inputTokens: llmResponse.InputTokens,
                    outputTokens: llmResponse.OutputTokens);

                activity?.SetSuccess(true);

                return new SendMessageResult(
                    SessionId: session.Id,
                    MessageId: assistantMessage.Id,
                    AssistantResponse: llmResponse.Content,
                    CreatedAt: assistantMessage.CreatedAt,
                    IsNewSession: isNewSession,
                    SessionTitle: session.Title);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }

    private List<LlmMessage> BuildLlmMessages(IReadOnlyList<Message> existingMessages)
    {
        var llmMessages = new List<LlmMessage>
        {
            // System prompt first
            new(MessageRole.System, SystemPrompts.CoachingPersonality)
        };

        // Add conversation history
        foreach (var message in existingMessages)
        {
            llmMessages.Add(new LlmMessage(message.Role, message.Content));
        }

        return llmMessages;
    }

    private async Task<string?> GenerateTitleAsync(
        string userMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var prompt = string.Format(SystemPrompts.TitleGeneration, userMessage);
            var messages = new List<LlmMessage>
            {
                new(MessageRole.User, prompt)
            };

            var response = await _llmService.GetChatCompletionAsync(messages, cancellationToken);

            // Clean up the response
            var title = response.Content
                .Trim()
                .Trim('"')
                .Trim();

            // Truncate if too long
            if (title.Length > ConversationSession.MaxTitleLength)
            {
                title = title[..ConversationSession.MaxTitleLength];
            }

            return title;
        }
        catch
        {
            // Title generation is not critical - return null if it fails
            return null;
        }
    }
}
```

### 7.2 SendMessageValidator

Following the pattern from existing validators:

```csharp
// File: Codewrinkles.Application/Nova/SendMessageValidator.cs

using Codewrinkles.Domain.Nova;
using Kommand.Validation;

namespace Codewrinkles.Application.Nova;

public sealed class SendMessageValidator : IValidator<SendMessageCommand>
{
    private List<ValidationError> _errors = [];

    public Task<ValidationResult> ValidateAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        _errors = [];

        // Validate ProfileId
        if (command.ProfileId == Guid.Empty)
        {
            _errors.Add(new ValidationError(
                nameof(command.ProfileId),
                "Profile ID is required."));
        }

        // Validate Content
        if (string.IsNullOrWhiteSpace(command.Content))
        {
            _errors.Add(new ValidationError(
                nameof(command.Content),
                "Message content is required."));
        }
        else if (command.Content.Length > Message.MaxContentLength)
        {
            _errors.Add(new ValidationError(
                nameof(command.Content),
                $"Message content cannot exceed {Message.MaxContentLength} characters."));
        }

        return Task.FromResult(_errors.Count > 0
            ? ValidationResult.Failure(_errors)
            : ValidationResult.Success());
    }
}
```

### 7.3 GetConversations Query

```csharp
// File: Codewrinkles.Application/Nova/GetConversations.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record GetConversationsQuery(
    Guid ProfileId,
    string? Cursor,
    int Limit
) : IQuery<GetConversationsResult>;

public sealed record GetConversationsResult(
    IReadOnlyList<ConversationSummaryDto> Conversations,
    string? NextCursor,
    bool HasMore
);

public sealed class GetConversationsQueryHandler
    : IQueryHandler<GetConversationsQuery, GetConversationsResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetConversationsResult> HandleAsync(
        GetConversationsQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.GetConversations);
        activity?.SetProfileId(query.ProfileId);

        // Parse cursor
        DateTimeOffset? beforeLastMessageAt = null;
        Guid? beforeId = null;

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            var parts = query.Cursor.Split('_');
            if (parts.Length == 2 &&
                DateTimeOffset.TryParse(parts[0], out var parsedDate) &&
                Guid.TryParse(parts[1], out var parsedId))
            {
                beforeLastMessageAt = parsedDate;
                beforeId = parsedId;
            }
        }

        // Fetch one extra to determine if there are more
        var sessions = await _unitOfWork.Nova.GetSessionsByProfileIdAsync(
            query.ProfileId,
            query.Limit + 1,
            beforeLastMessageAt,
            beforeId,
            cancellationToken);

        var hasMore = sessions.Count > query.Limit;
        var resultSessions = hasMore ? sessions.Take(query.Limit).ToList() : sessions;

        // Generate next cursor
        string? nextCursor = null;
        if (hasMore && resultSessions.Count > 0)
        {
            var last = resultSessions[^1];
            nextCursor = $"{last.LastMessageAt:O}_{last.Id}";
        }

        // Map to DTOs
        var dtos = resultSessions.Select(s => new ConversationSummaryDto(
            Id: s.Id,
            Title: s.Title ?? "New Conversation",
            LastMessageAt: s.LastMessageAt,
            CreatedAt: s.CreatedAt
        )).ToList();

        activity?.SetSuccess(true);

        return new GetConversationsResult(
            Conversations: dtos,
            NextCursor: nextCursor,
            HasMore: hasMore);
    }
}
```

### 7.4 GetConversation Query

```csharp
// File: Codewrinkles.Application/Nova/GetConversation.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova.Exceptions;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record GetConversationQuery(
    Guid ProfileId,
    Guid SessionId
) : IQuery<ConversationDto>;

public sealed class GetConversationQueryHandler
    : IQueryHandler<GetConversationQuery, ConversationDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetConversationQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConversationDto> HandleAsync(
        GetConversationQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.GetConversation);
        activity?.SetProfileId(query.ProfileId);
        activity?.SetEntity("ConversationSession", query.SessionId);

        var session = await _unitOfWork.Nova.FindSessionByIdWithMessagesAsync(
            query.SessionId,
            cancellationToken)
            ?? throw new ConversationNotFoundException(query.SessionId);

        // Verify ownership
        if (session.ProfileId != query.ProfileId)
        {
            throw new ConversationAccessDeniedException(query.SessionId, query.ProfileId);
        }

        if (session.IsDeleted)
        {
            throw new ConversationNotFoundException(query.SessionId);
        }

        // Map to DTO
        var messages = session.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(
                Id: m.Id,
                Role: m.Role.ToString().ToLowerInvariant(),
                Content: m.Content,
                CreatedAt: m.CreatedAt,
                TokensUsed: m.TokensUsed,
                ModelUsed: m.ModelUsed
            ))
            .ToList();

        activity?.SetSuccess(true);

        return new ConversationDto(
            Id: session.Id,
            Title: session.Title ?? "New Conversation",
            CreatedAt: session.CreatedAt,
            LastMessageAt: session.LastMessageAt,
            Messages: messages);
    }
}
```

### 7.5 DeleteConversation Command

```csharp
// File: Codewrinkles.Application/Nova/DeleteConversation.cs

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova.Exceptions;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record DeleteConversationCommand(
    Guid ProfileId,
    Guid SessionId
) : ICommand<DeleteConversationResult>;

public sealed record DeleteConversationResult(bool Success);

public sealed class DeleteConversationCommandHandler
    : ICommandHandler<DeleteConversationCommand, DeleteConversationResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteConversationCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteConversationResult> HandleAsync(
        DeleteConversationCommand command,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.DeleteConversation);
        activity?.SetProfileId(command.ProfileId);
        activity?.SetEntity("ConversationSession", command.SessionId);

        try
        {
            var session = await _unitOfWork.Nova.FindSessionByIdAsync(
                command.SessionId,
                cancellationToken)
                ?? throw new ConversationNotFoundException(command.SessionId);

            // Verify ownership
            if (session.ProfileId != command.ProfileId)
            {
                throw new ConversationAccessDeniedException(command.SessionId, command.ProfileId);
            }

            // Soft delete
            session.MarkAsDeleted();
            _unitOfWork.Nova.UpdateSession(session);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            activity?.SetSuccess(true);

            return new DeleteConversationResult(Success: true);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
```

### 7.6 DTOs

All DTOs in a single file following the `PulseDtos.cs` pattern:

```csharp
// File: Codewrinkles.Application/Nova/NovaDtos.cs

namespace Codewrinkles.Application.Nova;

public sealed record ConversationDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastMessageAt,
    IReadOnlyList<MessageDto> Messages
);

public sealed record ConversationSummaryDto(
    Guid Id,
    string Title,
    DateTimeOffset LastMessageAt,
    DateTimeOffset CreatedAt
);

public sealed record MessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    int? TokensUsed,
    string? ModelUsed
);

public sealed record SendMessageResponse(
    Guid SessionId,
    Guid MessageId,
    string Response,
    DateTimeOffset CreatedAt,
    bool IsNewSession,
    string? SessionTitle
);

public sealed record ConversationsResponse(
    IReadOnlyList<ConversationSummaryDto> Conversations,
    string? NextCursor,
    bool HasMore
);
```

---

## 8. API Layer - Endpoints

Following the pattern from `PulseEndpoints.cs`:

```csharp
// File: Codewrinkles.API/Modules/Nova/NovaEndpoints.cs

using Codewrinkles.Application.Nova.Commands;
using Codewrinkles.Application.Nova.Queries;
using Codewrinkles.API.Extensions;
using Codewrinkles.Domain.Nova.Exceptions;
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Nova;

public static class NovaEndpoints
{
    public static void MapNovaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/nova")
            .WithTags("Nova")
            .RequireAuthorization();

        // Chat endpoints
        group.MapPost("chat", SendMessage)
            .WithName("SendMessage");

        // Conversation management
        group.MapGet("conversations", GetConversations)
            .WithName("GetConversations");

        group.MapGet("conversations/{sessionId:guid}", GetConversation)
            .WithName("GetConversation");

        group.MapDelete("conversations/{sessionId:guid}", DeleteConversation)
            .WithName("DeleteConversation");
    }

    private static async Task<IResult> SendMessage(
        HttpContext httpContext,
        [FromBody] SendMessageRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var profileId = httpContext.GetCurrentProfileId();

            var command = new SendMessageCommand(
                ProfileId: profileId,
                SessionId: request.SessionId,
                Content: request.Message
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new
            {
                sessionId = result.SessionId,
                messageId = result.MessageId,
                response = result.AssistantResponse,
                createdAt = result.CreatedAt,
                isNewSession = result.IsNewSession,
                sessionTitle = result.SessionTitle
            });
        }
        catch (ConversationNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (ConversationAccessDeniedException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> GetConversations(
        HttpContext httpContext,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var profileId = httpContext.GetCurrentProfileId();

        // Default limit
        if (limit <= 0 || limit > 50)
        {
            limit = 20;
        }

        var query = new GetConversationsQuery(
            ProfileId: profileId,
            Cursor: cursor,
            Limit: limit
        );

        var result = await mediator.SendAsync(query, cancellationToken);

        return Results.Ok(new
        {
            conversations = result.Conversations,
            nextCursor = result.NextCursor,
            hasMore = result.HasMore
        });
    }

    private static async Task<IResult> GetConversation(
        HttpContext httpContext,
        Guid sessionId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var profileId = httpContext.GetCurrentProfileId();

            var query = new GetConversationQuery(
                ProfileId: profileId,
                SessionId: sessionId
            );

            var result = await mediator.SendAsync(query, cancellationToken);

            return Results.Ok(result);
        }
        catch (ConversationNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ConversationAccessDeniedException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> DeleteConversation(
        HttpContext httpContext,
        Guid sessionId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var profileId = httpContext.GetCurrentProfileId();

            var command = new DeleteConversationCommand(
                ProfileId: profileId,
                SessionId: sessionId
            );

            var result = await mediator.SendAsync(command, cancellationToken);

            return Results.Ok(new { success = result.Success });
        }
        catch (ConversationNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ConversationAccessDeniedException)
        {
            return Results.Forbid();
        }
    }
}

/// <summary>
/// Request body for sending a message.
/// </summary>
public sealed record SendMessageRequest(
    Guid? SessionId,
    string Message
);
```

### 8.1 Register Nova Endpoints

Add to Program.cs:

```csharp
// After other MapXxxEndpoints calls
app.MapNovaEndpoints();
```

---

## 9. Telemetry Integration

### 9.1 Add Nova Span Names

```csharp
// In Codewrinkles.Telemetry/SpanNames.cs - add

public static class Nova
{
    public const string SendMessage = "Nova.SendMessage";
    public const string GetConversations = "Nova.GetConversations";
    public const string GetConversation = "Nova.GetConversation";
    public const string DeleteConversation = "Nova.DeleteConversation";
}
```

### 9.2 Add Nova Metrics

```csharp
// In Codewrinkles.Telemetry/AppMetrics.cs - add

public static void RecordNovaMessage(bool isNewSession, int inputTokens, int outputTokens)
{
    // Record counters
    NovaMessagesCounter.Add(1);

    if (isNewSession)
    {
        NovaConversationsCounter.Add(1);
    }

    NovaTokensCounter.Add(inputTokens + outputTokens,
        new KeyValuePair<string, object?>("token_type", "total"));
}

// Add static counters
private static readonly Counter<long> NovaMessagesCounter =
    Meter.CreateCounter<long>("nova.messages.total");
private static readonly Counter<long> NovaConversationsCounter =
    Meter.CreateCounter<long>("nova.conversations.created");
private static readonly Counter<long> NovaTokensCounter =
    Meter.CreateCounter<long>("nova.tokens.total");
```

---

## 10. Exception Handling

### 10.1 Add Nova Exception Handler

```csharp
// File: Codewrinkles.API/ExceptionHandlers/NovaExceptionHandler.cs

using Codewrinkles.Domain.Nova.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.ExceptionHandlers;

public sealed class NovaExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ConversationNotFoundException notFoundEx)
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Conversation Not Found",
                    Detail = notFoundEx.Message
                },
                cancellationToken);
            return true;
        }

        if (exception is ConversationAccessDeniedException accessDeniedEx)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Access Denied",
                    Detail = "You do not have access to this conversation."
                },
                cancellationToken);
            return true;
        }

        return false;
    }
}
```

Register in Program.cs:

```csharp
builder.Services.AddExceptionHandler<NovaExceptionHandler>();
```

---

## 11. Configuration

### 11.1 User Secrets (Local Development)

**NEVER commit API keys to source control.**

```bash
cd apps/backend/src/Codewrinkles.API
dotnet user-secrets set "Nova:OpenAIApiKey" "sk-your-api-key-here"
```

### 11.2 appsettings.json (Non-sensitive defaults)

```json
{
  "Nova": {
    "ModelId": "gpt-4o-mini",
    "MaxTokens": 2048,
    "Temperature": 0.7,
    "MaxContextMessages": 20
  }
}
```

---

## 12. NuGet Packages to Add

```xml
<!-- In Codewrinkles.Infrastructure.csproj -->
<PackageReference Include="Microsoft.SemanticKernel" Version="1.68.0" />
```

---

## 13. Database Migration

After implementing the domain entities and configurations:

```bash
cd apps/backend/src/Codewrinkles.API
dotnet ef migrations add AddNovaSchema --project ../Codewrinkles.Infrastructure
dotnet ef database update
```

Expected tables:
- `nova.ConversationSessions`
- `nova.Messages`

---

## 14. Implementation Order

Following the principle of small, incremental steps:

### Phase 1: Domain Layer (Day 1)
1. [ ] Create `MessageRole.cs` enum
2. [ ] Create `Message.cs` entity
3. [ ] Create `ConversationSession.cs` entity
4. [ ] Create domain exceptions
5. [ ] Build and verify no errors

### Phase 2: Infrastructure - Database (Day 1-2)
1. [ ] Add `MessageConfiguration.cs`
2. [ ] Add `ConversationSessionConfiguration.cs`
3. [ ] Update `ApplicationDbContext` with DbSets
4. [ ] Create migration
5. [ ] Run migration, verify tables created

### Phase 3: Infrastructure - Repository (Day 2)
1. [ ] Create `INovaRepository.cs` interface
2. [ ] Implement `NovaRepository.cs`
3. [ ] Update `IUnitOfWork.cs` with Nova property
4. [ ] Update `UnitOfWork.cs` implementation
5. [ ] Build and verify

### Phase 4: Infrastructure - LLM Service (Day 2-3)
1. [ ] Add Semantic Kernel NuGet package
2. [ ] Create `NovaSettings.cs`
3. [ ] Create `ILlmService.cs` interface
4. [ ] Implement `SemanticKernelLlmService.cs`
5. [ ] Add DI registration
6. [ ] Configure User Secrets with API key
7. [ ] Test LLM service independently

### Phase 5: Application Layer (Day 3-4)
1. [ ] Create `SystemPrompts.cs`
2. [ ] Create DTOs
3. [ ] Implement `SendMessage.cs` command + handler + validator
4. [ ] Implement `GetConversations.cs` query + handler
5. [ ] Implement `GetConversation.cs` query + handler
6. [ ] Implement `DeleteConversation.cs` command + handler
7. [ ] Build and verify

### Phase 6: API Layer (Day 4)
1. [ ] Create `NovaEndpoints.cs`
2. [ ] Add `NovaExceptionHandler.cs`
3. [ ] Register endpoints in Program.cs
4. [ ] Add telemetry span names and metrics
5. [ ] Test endpoints with Scalar

### Phase 7: Integration Testing (Day 5)
1. [ ] Test full flow: Create conversation → Send messages → Get conversation
2. [ ] Test error cases: Not found, access denied
3. [ ] Test conversation history context
4. [ ] Verify telemetry metrics
5. [ ] Load test with multiple messages

---

## 15. Future Considerations (Not in Milestone 1)

### Streaming Responses (Milestone 1.10)
The `ILlmService.GetStreamingChatCompletionAsync` method is implemented but not exposed via API yet. For streaming:
- Add Server-Sent Events (SSE) endpoint
- Or use SignalR for WebSocket streaming
- Frontend needs to handle chunked responses

### Rate Limiting
- Per-user message limits (e.g., 50 messages/day for free tier)
- Token usage tracking and limits
- Consider implementing in API middleware

### Context Window Management
- Current: Simple truncation to last N messages
- Future: Summarization of older messages
- Future: Sliding window with key information retention

---

## 16. Testing Strategy

### Unit Tests
- Domain entity creation and validation
- Validators for commands/queries
- LLM message building logic

### Integration Tests
- Repository operations
- Command/query handlers with in-memory database
- LLM service with mock responses

### E2E Tests
- Full API flow with real database
- Authentication and authorization
- Error handling

---

## Appendix A: File Checklist

```
Domain Layer:
[ ] Domain/Nova/MessageRole.cs
[ ] Domain/Nova/Message.cs
[ ] Domain/Nova/ConversationSession.cs
[ ] Domain/Nova/Exceptions/ConversationNotFoundException.cs
[ ] Domain/Nova/Exceptions/ConversationAccessDeniedException.cs

Infrastructure Layer:
[ ] Infrastructure/Persistence/Configurations/Nova/MessageConfiguration.cs
[ ] Infrastructure/Persistence/Configurations/Nova/ConversationSessionConfiguration.cs
[ ] Infrastructure/Persistence/Repositories/Nova/NovaRepository.cs    # Note: Nova subfolder
[ ] Infrastructure/Services/Nova/NovaSettings.cs
[ ] Infrastructure/Services/Nova/SemanticKernelLlmService.cs

Application Layer (flat feature folder - NO subfolders):
[ ] Application/Common/Interfaces/INovaRepository.cs
[ ] Application/Common/Interfaces/ILlmService.cs
[ ] Application/Nova/SystemPrompts.cs           # Flat in Nova folder
[ ] Application/Nova/NovaDtos.cs                # All DTOs in one file
[ ] Application/Nova/SendMessage.cs             # Command + Handler + Result
[ ] Application/Nova/SendMessageValidator.cs
[ ] Application/Nova/DeleteConversation.cs      # Command + Handler + Result
[ ] Application/Nova/DeleteConversationValidator.cs
[ ] Application/Nova/GetConversations.cs        # Query + Handler + Result
[ ] Application/Nova/GetConversation.cs         # Query + Handler + Result

API Layer:
[ ] API/Modules/Nova/NovaEndpoints.cs
[ ] API/ExceptionHandlers/NovaExceptionHandler.cs
```

---

## Appendix B: API Contract Summary

### POST /api/nova/chat
**Request:**
```json
{
  "sessionId": "guid | null",
  "message": "string"
}
```
**Response:**
```json
{
  "sessionId": "guid",
  "messageId": "guid",
  "response": "string",
  "createdAt": "datetime",
  "isNewSession": "boolean",
  "sessionTitle": "string | null"
}
```

### GET /api/nova/conversations?cursor=&limit=
**Response:**
```json
{
  "conversations": [{
    "id": "guid",
    "title": "string",
    "lastMessageAt": "datetime",
    "createdAt": "datetime"
  }],
  "nextCursor": "string | null",
  "hasMore": "boolean"
}
```

### GET /api/nova/conversations/{sessionId}
**Response:**
```json
{
  "id": "guid",
  "title": "string",
  "createdAt": "datetime",
  "lastMessageAt": "datetime",
  "messages": [{
    "id": "guid",
    "role": "user | assistant | system",
    "content": "string",
    "createdAt": "datetime",
    "tokensUsed": "int | null",
    "modelUsed": "string | null"
  }]
}
```

### DELETE /api/nova/conversations/{sessionId}
**Response:**
```json
{
  "success": "boolean"
}
```

---

**Document Complete. Ready for implementation.**
