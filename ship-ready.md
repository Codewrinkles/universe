# Ship-Ready Features - Pulse MVP Launch

> **Purpose**: Technical implementation plan and status tracking for Pulse MVP launch features. This document follows all patterns from CLAUDE.md and the existing codebase.

**Last Updated**: 2025-11-30

---

## 📊 Implementation Status

### ✅ **COMPLETED FEATURES**

All core features required for invitation-only MVP launch are **COMPLETE**:

1. **✅ Link Preview System** - DONE
   - Domain entity: `PulseLinkPreview.cs`
   - Service: `LinkPreviewService.cs` with Open Graph parsing
   - Integration: Active in `CreatePulse` handler
   - Migration: Applied
   - **Evidence**: Links in pulses automatically generate rich previews

2. **✅ Hashtags & Popular Topics** - DONE
   - Domain entities: `Hashtag.cs`, `PulseHashtag.cs`
   - Repository: `HashtagRepository.cs`
   - Queries: `GetTrendingHashtags`, `GetPulsesByHashtag`
   - Frontend: `TrendingTopics.tsx` fully functional
   - Migration: `20251129191226_AddHashtags` applied
   - **Evidence**: Hashtags extracted from pulse content, trending sidebar populated, hashtag pages work

3. **✅ Admin System & Dashboard** - DONE
   - Domain: `UserRole` enum with Admin/User roles
   - Endpoints: `AdminEndpoints.cs` with authorization policy "AdminOnly"
   - Metrics: `GetDashboardMetrics` query implemented
   - Frontend: `DashboardPage.tsx` with metrics grid
   - Navigation: Admin app appears in AppSwitcher for admin users
   - **Evidence**: Admin dashboard accessible at `/admin`, role-based access control working

4. **✅ Onboarding Flow** - DONE
   - Domain: `Profile.OnboardingCompleted` flag
   - Frontend: `OnboardingFlow.tsx` with 3-step wizard
   - Steps: Complete Profile → First Pulse → Suggested Follows
   - API: `/api/identity/onboarding/status` and `/onboarding/complete` endpoints
   - **Evidence**: New users guided through onboarding, redirected to main feed on completion

### ⏭️ **DEFERRED FEATURES**

1. **⏭️ Pulse Reporting System** - INTENTIONALLY SKIPPED
   - **Status**: Not implemented
   - **Rationale**: Product will launch as **invitation-only**
   - **Why skip**:
     - With a curated, invited community, moderation can be handled manually
     - Admin can delete pulses directly if needed via dashboard/database
     - Reduces development time and complexity for MVP
     - Can be added post-launch if community grows beyond manual moderation capacity
   - **Future consideration**: Implement when transitioning to public registration

---

## 🏗️ Architectural Principles

**CRITICAL: Interface Ownership (SOLID - Dependency Inversion Principle)**

All service interfaces MUST be owned by the **consumer** (Application layer), NOT the implementation (Infrastructure layer):

- ✅ **Interfaces**: `Codewrinkles.Application/Common/Interfaces/`
- ✅ **Implementations**: `Codewrinkles.Infrastructure/Services/`
- ✅ **DTOs/Records**: Co-located with interface (Application layer)

**Example**:
```
Application/Common/Interfaces/ILinkPreviewService.cs  ← Interface + DTOs
Infrastructure/Services/LinkPreviewService.cs         ← Implementation
```

**Why**: The Application layer defines what it needs (the contract). Infrastructure provides how it's done (the implementation). This follows the Dependency Inversion Principle and matches the existing pattern for `IUnitOfWork`, `IPulseRepository`, etc.

---

## Table of Contents

1. [Implementation Status](#-implementation-status) ⭐ **START HERE**
2. [Link Preview System](#1-link-preview-system) ✅ COMPLETE
3. [Hashtags & Popular Topics](#2-hashtags--popular-topics) ✅ COMPLETE
4. [Who to Follow](#3-who-to-follow---status-confirmed) ✅ COMPLETE
5. [Onboarding Flow](#4-onboarding-flow) ✅ COMPLETE
6. [Admin System & Dashboard](#5-admin-system--dashboard) ✅ COMPLETE
7. [Pulse Reporting System](#6-pulse-reporting-system) ⏭️ DEFERRED
8. [Ready to Ship](#ready-to-ship) 🚀

---

## 1. Link Preview System

### **Goal**
Auto-generate rich link previews (Open Graph) when users post URLs in pulse content.

### **Database Schema**

> **⚠️ CRITICAL WARNING: DO NOT EXECUTE THESE SQL SCRIPTS**
>
> The SQL scripts below are **for documentation and reference purposes ONLY**.
>
> **ALL database schema changes MUST be implemented through:**
> 1. Entity classes in `Codewrinkles.Domain/Pulse/`
> 2. EF Core configurations in `Codewrinkles.Infrastructure/Persistence/Configurations/Pulse/`
> 3. EF Core migrations: `dotnet ef migrations add <MigrationName>`
> 4. Apply migrations: `dotnet ef database update`
>
> **Executing raw SQL DDL statements is strictly forbidden.**
>
> These scripts exist only to visualize the target schema structure.

#### `pulse.PulseLinkPreviews` (Reference Only - Use EF Migrations)

```sql
CREATE TABLE pulse.PulseLinkPreviews (
    Id              UNIQUEIDENTIFIER    PRIMARY KEY,
    PulseId         UNIQUEIDENTIFIER    NOT NULL,   -- FK → pulse.Pulses
    Url             NVARCHAR(2000)      NOT NULL,
    Title           NVARCHAR(200)       NOT NULL,
    Description     NVARCHAR(500)       NULL,
    ImageUrl        NVARCHAR(500)       NULL,
    Domain          NVARCHAR(100)       NOT NULL,
    CreatedAt       DATETIME2           NOT NULL,

    CONSTRAINT FK_PulseLinkPreviews_Pulse FOREIGN KEY (PulseId)
        REFERENCES pulse.Pulses(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_PulseLinkPreviews_PulseId ON pulse.PulseLinkPreviews(PulseId);
```

### **Backend Implementation**

#### **Domain Layer**

**File**: `apps/backend/src/Codewrinkles.Domain/Pulse/PulseLinkPreview.cs`

```csharp
namespace Codewrinkles.Domain.Pulse;

public sealed class PulseLinkPreview
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private PulseLinkPreview() { } // EF Core constructor
#pragma warning restore CS8618

    public Guid Id { get; private set; }
    public Guid PulseId { get; private set; }
    public string Url { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public string Domain { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public Pulse Pulse { get; private set; }

    public static PulseLinkPreview Create(
        Guid pulseId,
        string url,
        string title,
        string domain,
        string? description = null,
        string? imageUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(domain, nameof(domain));

        return new PulseLinkPreview
        {
            // Id generated by EF Core (sequential GUID)
            PulseId = pulseId,
            Url = url.Trim(),
            Title = title.Trim(),
            Description = description?.Trim(),
            ImageUrl = imageUrl?.Trim(),
            Domain = domain.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

#### **Infrastructure Layer**

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Pulse/PulseLinkPreviewConfiguration.cs`

```csharp
using Codewrinkles.Domain.Pulse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class PulseLinkPreviewConfiguration : IEntityTypeConfiguration<PulseLinkPreview>
{
    public void Configure(EntityTypeBuilder<PulseLinkPreview> builder)
    {
        builder.ToTable("PulseLinkPreviews", "pulse");

        builder.HasKey(p => p.Id);

        // Sequential GUID generation
        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Url)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(p => p.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Domain)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(p => p.Pulse)
            .WithOne()
            .HasForeignKey<PulseLinkPreview>(p => p.PulseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index: one link preview per pulse
        builder.HasIndex(p => p.PulseId)
            .IsUnique();
    }
}
```

**Update**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/ApplicationDbContext.cs`

```csharp
// Add to DbSets
public DbSet<PulseLinkPreview> PulseLinkPreviews => Set<PulseLinkPreview>();
```

**Migration**: Create migration `AddPulseLinkPreviews`

```bash
cd apps/backend/src/Codewrinkles.API
dotnet ef migrations add AddPulseLinkPreviews --project ../Codewrinkles.Infrastructure
dotnet ef database update
```

#### **Application Layer - Link Preview Service Interface**

**File**: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/ILinkPreviewService.cs`

```csharp
namespace Codewrinkles.Application.Common.Interfaces;

public interface ILinkPreviewService
{
    Task<LinkPreviewData?> FetchPreviewAsync(string url, CancellationToken cancellationToken);
    string? ExtractFirstUrl(string content);
}

public sealed record LinkPreviewData(
    string Url,
    string Title,
    string Domain,
    string? Description,
    string? ImageUrl);
```

#### **Infrastructure - Link Preview Service Implementation**

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Services/LinkPreviewService.cs`

```csharp
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Codewrinkles.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Codewrinkles.Infrastructure.Services;

public sealed class LinkPreviewService : ILinkPreviewService
{
    private static readonly Regex UrlRegex = new(
        @"https?://[^\s]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly HttpClient _httpClient;
    private readonly ILogger<LinkPreviewService> _logger;

    public LinkPreviewService(
        IHttpClientFactory httpClientFactory,
        ILogger<LinkPreviewService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("LinkPreview");
        _logger = logger;
    }

    public string? ExtractFirstUrl(string content)
    {
        var match = UrlRegex.Match(content);
        return match.Success ? match.Value : null;
    }

    public async Task<LinkPreviewData?> FetchPreviewAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate URL
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            // Fetch HTML
            var response = await _httpClient.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            // Parse Open Graph tags
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = GetMetaContent(doc, "og:title")
                ?? doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim()
                ?? uri.Host;

            var description = GetMetaContent(doc, "og:description")
                ?? GetMetaContent(doc, "description");

            var imageUrl = GetMetaContent(doc, "og:image");

            return new LinkPreviewData(
                Url: url,
                Title: title,
                Domain: uri.Host,
                Description: description,
                ImageUrl: imageUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch link preview for URL: {Url}", url);
            return null;
        }
    }

    private static string? GetMetaContent(HtmlDocument doc, string property)
    {
        var node = doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']")
            ?? doc.DocumentNode.SelectSingleNode($"//meta[@name='{property}']");

        return node?.GetAttributeValue("content", null)?.Trim();
    }
}
```

**Update**: `apps/backend/src/Codewrinkles.API/Program.cs`

```csharp
// Register LinkPreviewService
builder.Services.AddHttpClient("LinkPreview", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.Add("User-Agent", "Codewrinkles/1.0");
});
builder.Services.AddScoped<ILinkPreviewService, LinkPreviewService>();
```

**NuGet Package**: Install `HtmlAgilityPack`

```bash
cd apps/backend/src/Codewrinkles.Infrastructure
dotnet add package HtmlAgilityPack
```

#### **Application Layer - Update CreatePulse**

**Update**: `apps/backend/src/Codewrinkles.Application/Pulse/CreatePulse.cs`

```csharp
// Add ILinkPreviewService to constructor
private readonly ILinkPreviewService _linkPreviewService;

public CreatePulseCommandHandler(
    IUnitOfWork unitOfWork,
    IPulseImageService pulseImageService,
    ILinkPreviewService linkPreviewService)
{
    _unitOfWork = unitOfWork;
    _pulseImageService = pulseImageService;
    _linkPreviewService = linkPreviewService;
}

// In HandleAsync, after creating pulse and image:

// 4. Generate link preview if URL detected
string? firstUrl = _linkPreviewService.ExtractFirstUrl(command.Content);
if (firstUrl != null)
{
    var previewData = await _linkPreviewService.FetchPreviewAsync(
        firstUrl,
        cancellationToken);

    if (previewData != null)
    {
        var linkPreview = PulseLinkPreview.Create(
            pulseId: pulse.Id,
            url: previewData.Url,
            title: previewData.Title,
            domain: previewData.Domain,
            description: previewData.Description,
            imageUrl: previewData.ImageUrl);

        _unitOfWork.Pulses.CreateLinkPreview(linkPreview);
    }
}
```

**Update Repository**: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IPulseRepository.cs`

```csharp
void CreateLinkPreview(PulseLinkPreview linkPreview);
```

**Update Repository**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/PulseRepository.cs`

```csharp
public void CreateLinkPreview(PulseLinkPreview linkPreview)
{
    _linkPreviews.Add(linkPreview);
}

// Add to constructor
private readonly DbSet<PulseLinkPreview> _linkPreviews;

public PulseRepository(ApplicationDbContext context, IDbContextFactory<ApplicationDbContext> contextFactory)
{
    _context = context;
    _contextFactory = contextFactory;
    _pulses = context.Set<Pulse>();
    _pulseLikes = context.Set<PulseLike>();
    _pulseImages = context.Set<PulseImage>();
    _pulseEngagements = context.Set<PulseEngagement>();
    _pulseMentions = context.Set<PulseMention>();
    _pulseBookmarks = context.Set<PulseBookmark>();
    _linkPreviews = context.Set<PulseLinkPreview>(); // NEW
}
```

**Update DTOs**: `apps/backend/src/Codewrinkles.Application/Pulse/PulseDtos.cs`

```csharp
public sealed record PulseLinkPreviewDto(
    string Url,
    string Title,
    string? Description,
    string? ImageUrl,
    string Domain);

// Update PulseDto to include:
public sealed record PulseDto(
    // ... existing properties
    PulseLinkPreviewDto? LinkPreview  // NEW
);
```

**Update Query Handlers**: Include link preview in `.Include()` chains

```csharp
// In GetFeed, GetPulse, GetThread queries:
.Include(p => p.LinkPreview)  // Add this
```

### **Frontend - Already Exists!**

The component `apps/frontend/src/features/pulse/PostLinkPreview.tsx` already exists and just needs the data from the backend.

**Update**: `apps/frontend/src/features/pulse/types.ts`

```typescript
export interface PulseLinkPreview {
  url: string;
  title: string;
  description?: string;
  imageUrl?: string;
  domain: string;
}

// Update Pulse interface to include:
linkPreview?: PulseLinkPreview;
```

---

## 2. Hashtags & Popular Topics

### **Goal**
Allow users to use hashtags (#topic) in pulses. Display popular hashtags in right sidebar. Clicking a hashtag shows pulses with that tag.

### **Database Schema**

> **⚠️ CRITICAL WARNING: DO NOT EXECUTE THESE SQL SCRIPTS**
>
> The SQL scripts below are **for documentation and reference purposes ONLY**.
>
> **ALL database schema changes MUST be implemented through:**
> 1. Entity classes in `Codewrinkles.Domain/Pulse/`
> 2. EF Core configurations in `Codewrinkles.Infrastructure/Persistence/Configurations/Pulse/`
> 3. EF Core migrations: `dotnet ef migrations add <MigrationName>`
> 4. Apply migrations: `dotnet ef database update`
>
> **Executing raw SQL DDL statements is strictly forbidden.**
>
> These scripts exist only to visualize the target schema structure.

#### `pulse.Hashtags` (Reference Only - Use EF Migrations)

```sql
CREATE TABLE pulse.Hashtags (
    Id              UNIQUEIDENTIFIER    PRIMARY KEY,
    Tag             NVARCHAR(100)       NOT NULL,   -- Normalized (lowercase, no #)
    TagDisplay      NVARCHAR(100)       NOT NULL,   -- Original case (for display)
    PulseCount      INT                 NOT NULL DEFAULT 0,
    LastUsedAt      DATETIME2           NOT NULL,
    CreatedAt       DATETIME2           NOT NULL,

    CONSTRAINT UQ_Hashtags_Tag UNIQUE (Tag)
);

CREATE INDEX IX_Hashtags_PulseCount_LastUsedAt ON pulse.Hashtags(PulseCount DESC, LastUsedAt DESC);
```

#### `pulse.PulseHashtags` - Join Table (Reference Only - Use EF Migrations)

```sql
CREATE TABLE pulse.PulseHashtags (
    PulseId     UNIQUEIDENTIFIER    NOT NULL,   -- FK → pulse.Pulses
    HashtagId   UNIQUEIDENTIFIER    NOT NULL,   -- FK → pulse.Hashtags
    Position    INT                 NOT NULL,   -- Order in content
    CreatedAt   DATETIME2           NOT NULL,

    CONSTRAINT PK_PulseHashtags PRIMARY KEY (PulseId, HashtagId),
    CONSTRAINT FK_PulseHashtags_Pulse FOREIGN KEY (PulseId)
        REFERENCES pulse.Pulses(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PulseHashtags_Hashtag FOREIGN KEY (HashtagId)
        REFERENCES pulse.Hashtags(Id) ON DELETE CASCADE
);

CREATE INDEX IX_PulseHashtags_HashtagId ON pulse.PulseHashtags(HashtagId);
```

### **Backend Implementation**

#### **Domain Layer**

**File**: `apps/backend/src/Codewrinkles.Domain/Pulse/Hashtag.cs`

```csharp
namespace Codewrinkles.Domain.Pulse;

public sealed class Hashtag
{
#pragma warning disable CS8618
    private Hashtag() { } // EF Core constructor
#pragma warning restore CS8618

    public Guid Id { get; private set; }
    public string Tag { get; private set; }
    public string TagDisplay { get; private set; }
    public int PulseCount { get; private set; }
    public DateTime LastUsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public ICollection<PulseHashtag> PulseHashtags { get; private set; } = [];

    public static Hashtag Create(string tagDisplay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tagDisplay, nameof(tagDisplay));

        var normalized = tagDisplay.Trim().ToLowerInvariant().TrimStart('#');

        return new Hashtag
        {
            Tag = normalized,
            TagDisplay = tagDisplay.Trim().TrimStart('#'),
            PulseCount = 0,
            LastUsedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void IncrementUsage()
    {
        PulseCount++;
        LastUsedAt = DateTime.UtcNow;
    }

    public void DecrementUsage()
    {
        if (PulseCount > 0)
            PulseCount--;
    }
}
```

**File**: `apps/backend/src/Codewrinkles.Domain/Pulse/PulseHashtag.cs`

```csharp
namespace Codewrinkles.Domain.Pulse;

public sealed class PulseHashtag
{
#pragma warning disable CS8618
    private PulseHashtag() { } // EF Core constructor
#pragma warning restore CS8618

    public Guid PulseId { get; private set; }
    public Guid HashtagId { get; private set; }
    public int Position { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Pulse Pulse { get; private set; }
    public Hashtag Hashtag { get; private set; }

    public static PulseHashtag Create(Guid pulseId, Guid hashtagId, int position)
    {
        return new PulseHashtag
        {
            PulseId = pulseId,
            HashtagId = hashtagId,
            Position = position,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

#### **Application Layer - Hashtag Service Interface**

**File**: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IHashtagService.cs`

```csharp
namespace Codewrinkles.Application.Common.Interfaces;

public interface IHashtagService
{
    List<string> ExtractHashtags(string content);
}
```

#### **Infrastructure - Hashtag Service Implementation**

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Services/HashtagService.cs`

```csharp
using System.Text.RegularExpressions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Infrastructure.Services;

public sealed class HashtagService : IHashtagService
{
    private static readonly Regex HashtagRegex = new(
        @"(?:^|\s)#([a-zA-Z0-9_]+)(?=\s|$|[.,!?;:])",
        RegexOptions.Compiled);

    public List<string> ExtractHashtags(string content)
    {
        var matches = HashtagRegex.Matches(content);
        return matches
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
```

**Register in Program.cs**:

```csharp
builder.Services.AddScoped<IHashtagService, HashtagService>();
```

#### **Repository Methods**

**Update**: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IPulseRepository.cs`

```csharp
// Hashtag operations
Task<Hashtag?> FindHashtagByTagAsync(string tag, CancellationToken cancellationToken);
void CreateHashtag(Hashtag hashtag);
void CreatePulseHashtag(PulseHashtag pulseHashtag);
Task<IReadOnlyList<Hashtag>> GetTrendingHashtagsAsync(int limit, CancellationToken cancellationToken);
Task<(IReadOnlyList<PulseDto> Pulses, string? NextCursor, bool HasMore)> GetPulsesByHashtagAsync(
    string tag,
    Guid? currentUserId,
    string? cursor,
    int limit,
    CancellationToken cancellationToken);
```

**Implement in**: `PulseRepository.cs`

```csharp
private readonly DbSet<Hashtag> _hashtags;
private readonly DbSet<PulseHashtag> _pulseHashtags;

public async Task<Hashtag?> FindHashtagByTagAsync(string tag, CancellationToken cancellationToken)
{
    var normalized = tag.ToLowerInvariant().TrimStart('#');
    return await _hashtags
        .FirstOrDefaultAsync(h => h.Tag == normalized, cancellationToken);
}

public void CreateHashtag(Hashtag hashtag)
{
    _hashtags.Add(hashtag);
}

public void CreatePulseHashtag(PulseHashtag pulseHashtag)
{
    _pulseHashtags.Add(pulseHashtag);
}

public async Task<IReadOnlyList<Hashtag>> GetTrendingHashtagsAsync(
    int limit,
    CancellationToken cancellationToken)
{
    // Get hashtags used in the last 7 days, ordered by usage
    var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

    return await _hashtags
        .Where(h => h.LastUsedAt >= sevenDaysAgo)
        .OrderByDescending(h => h.PulseCount)
        .ThenByDescending(h => h.LastUsedAt)
        .Take(limit)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}

public async Task<(IReadOnlyList<PulseDto> Pulses, string? NextCursor, bool HasMore)> GetPulsesByHashtagAsync(
    string tag,
    Guid? currentUserId,
    string? cursor,
    int limit,
    CancellationToken cancellationToken)
{
    var normalized = tag.ToLowerInvariant().TrimStart('#');

    // Base query: pulses with this hashtag
    var query = _pulses
        .Where(p => !p.IsDeleted)
        .Where(p => p.PulseHashtags.Any(ph => ph.Hashtag.Tag == normalized))
        .Include(p => p.Author)
        .Include(p => p.Engagement)
        .Include(p => p.Image)
        .Include(p => p.LinkPreview)
        .Include(p => p.RepulsedPulse)
            .ThenInclude(rp => rp!.Author)
        .AsNoTracking();

    // Apply cursor pagination (same logic as GetFeedAsync)
    if (!string.IsNullOrEmpty(cursor))
    {
        var parts = cursor.Split('_');
        if (parts.Length == 2 &&
            DateTime.TryParse(parts[0], out var createdAt) &&
            Guid.TryParse(parts[1], out var id))
        {
            query = query.Where(p => p.CreatedAt < createdAt ||
                (p.CreatedAt == createdAt && p.Id.CompareTo(id) < 0));
        }
    }

    query = query
        .OrderByDescending(p => p.CreatedAt)
        .ThenByDescending(p => p.Id);

    var pulses = await query.Take(limit + 1).ToListAsync(cancellationToken);

    var hasMore = pulses.Count > limit;
    if (hasMore)
        pulses = pulses.Take(limit).ToList();

    // Get metadata (likes, bookmarks) if authenticated
    // (Same parallel query logic as GetFeedAsync)
    var pulseDtos = await MapToDtosWithMetadata(pulses, currentUserId, cancellationToken);

    var nextCursor = hasMore && pulses.Count > 0
        ? $"{pulses[^1].CreatedAt:O}_{pulses[^1].Id}"
        : null;

    return (pulseDtos, nextCursor, hasMore);
}
```

#### **Application Layer - Update CreatePulse**

**Update**: `apps/backend/src/Codewrinkles.Application/Pulse/CreatePulse.cs`

```csharp
// Add IHashtagService to constructor
private readonly IHashtagService _hashtagService;

// In HandleAsync, after saving pulse:

// 5. Extract and create hashtags
var hashtags = _hashtagService.ExtractHashtags(command.Content);
foreach (var (tag, index) in hashtags.Select((t, i) => (t, i)))
{
    // Find or create hashtag
    var hashtag = await _unitOfWork.Pulses.FindHashtagByTagAsync(tag, cancellationToken);
    if (hashtag == null)
    {
        hashtag = Hashtag.Create(tag);
        _unitOfWork.Pulses.CreateHashtag(hashtag);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // Save to get ID
    }

    hashtag.IncrementUsage();

    // Link pulse to hashtag
    var pulseHashtag = PulseHashtag.Create(pulse.Id, hashtag.Id, index);
    _unitOfWork.Pulses.CreatePulseHashtag(pulseHashtag);
}
```

#### **Application Layer - New Queries**

**File**: `apps/backend/src/Codewrinkles.Application/Pulse/GetTrendingHashtags.cs`

```csharp
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Pulse;

public sealed record GetTrendingHashtagsQuery(int Limit = 10) : ICommand<TrendingHashtagsResponse>;

public sealed record TrendingHashtagsResponse(IReadOnlyList<HashtagDto> Hashtags);

public sealed record HashtagDto(string Tag, int PulseCount);

public sealed class GetTrendingHashtagsQueryHandler
    : ICommandHandler<GetTrendingHashtagsQuery, TrendingHashtagsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTrendingHashtagsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TrendingHashtagsResponse> HandleAsync(
        GetTrendingHashtagsQuery query,
        CancellationToken cancellationToken)
    {
        var trending = await _unitOfWork.Pulses.GetTrendingHashtagsAsync(
            query.Limit,
            cancellationToken);

        var dtos = trending.Select(h => new HashtagDto(
            Tag: h.TagDisplay,
            PulseCount: h.PulseCount
        )).ToList();

        return new TrendingHashtagsResponse(dtos);
    }
}
```

**File**: `apps/backend/src/Codewrinkles.Application/Pulse/GetPulsesByHashtag.cs`

```csharp
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Pulse;

public sealed record GetPulsesByHashtagQuery(
    string Tag,
    Guid? CurrentUserId,
    string? Cursor,
    int Limit = 20
) : ICommand<FeedResponse>;

public sealed class GetPulsesByHashtagQueryHandler
    : ICommandHandler<GetPulsesByHashtagQuery, FeedResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPulsesByHashtagQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FeedResponse> HandleAsync(
        GetPulsesByHashtagQuery query,
        CancellationToken cancellationToken)
    {
        var (pulses, nextCursor, hasMore) = await _unitOfWork.Pulses.GetPulsesByHashtagAsync(
            query.Tag,
            query.CurrentUserId,
            query.Cursor,
            query.Limit,
            cancellationToken);

        return new FeedResponse(pulses, nextCursor, hasMore);
    }
}
```

#### **API Endpoints**

**Update**: `apps/backend/src/Codewrinkles.API/Modules/Pulse/PulseEndpoints.cs`

```csharp
// Add new endpoints

// GET /api/pulse/hashtags/trending
group.MapGet("/hashtags/trending", async (
    [FromServices] ICommander commander,
    [FromQuery] int limit = 10,
    CancellationToken cancellationToken = default) =>
{
    var query = new GetTrendingHashtagsQuery(limit);
    var result = await commander.ExecuteAsync(query, cancellationToken);
    return Results.Ok(result);
})
.WithName("GetTrendingHashtags")
.WithTags("Pulse");

// GET /api/pulse/hashtag/{tag}
group.MapGet("/hashtag/{tag}", async (
    [FromRoute] string tag,
    [FromServices] ICommander commander,
    [FromQuery] string? cursor,
    [FromQuery] int limit = 20,
    HttpContext httpContext,
    CancellationToken cancellationToken = default) =>
{
    var currentUserId = httpContext.User.GetUserId();

    var query = new GetPulsesByHashtagQuery(
        Tag: tag,
        CurrentUserId: currentUserId,
        Cursor: cursor,
        Limit: limit);

    var result = await commander.ExecuteAsync(query, cancellationToken);
    return Results.Ok(result);
})
.WithName("GetPulsesByHashtag")
.WithTags("Pulse");
```

### **Frontend Implementation**

**Update**: `apps/frontend/src/features/pulse/TrendingTopics.tsx` (already exists, make it functional)

```typescript
import { useEffect, useState } from "react";
import { Card } from "../../components/ui/Card";
import { Link } from "react-router-dom";

interface Hashtag {
  tag: string;
  pulseCount: number;
}

export function TrendingTopics(): JSX.Element {
  const [hashtags, setHashtags] = useState<Hashtag[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function fetchTrending() {
      try {
        const response = await fetch('/api/pulse/hashtags/trending?limit=5');
        const data = await response.json();
        setHashtags(data.hashtags);
      } catch (error) {
        console.error('Failed to fetch trending:', error);
      } finally {
        setIsLoading(false);
      }
    }

    void fetchTrending();
  }, []);

  if (isLoading) {
    return (
      <Card>
        <h2 className="text-base font-semibold tracking-tight text-text-primary mb-4">
          Popular Topics
        </h2>
        <p className="text-xs text-text-tertiary">Loading...</p>
      </Card>
    );
  }

  if (hashtags.length === 0) {
    return <></>;
  }

  return (
    <Card>
      <h2 className="text-base font-semibold tracking-tight text-text-primary mb-4">
        Popular Topics
      </h2>
      <div className="space-y-3">
        {hashtags.map((hashtag) => (
          <Link
            key={hashtag.tag}
            to={`/social/hashtag/${hashtag.tag}`}
            className="block hover:bg-surface-card2 rounded-lg p-3 -mx-3 transition-colors"
          >
            <div className="flex items-baseline justify-between">
              <span className="text-sm font-medium text-brand">
                #{hashtag.tag}
              </span>
              <span className="text-xs text-text-tertiary">
                {hashtag.pulseCount} {hashtag.pulseCount === 1 ? 'pulse' : 'pulses'}
              </span>
            </div>
          </Link>
        ))}
      </div>
    </Card>
  );
}
```

**New Route**: Add to `apps/frontend/src/App.tsx`

```typescript
<Route path="/social/hashtag/:tag" element={<HashtagPage />} />
```

**New Page**: `apps/frontend/src/features/pulse/HashtagPage.tsx`

```typescript
import { useParams } from "react-router-dom";
import { useFeed } from "./hooks/useFeed";
import { Feed } from "./Feed";
import { PulseNavigation } from "./PulseNavigation";
import { PulseRightSidebar } from "./PulseRightSidebar";

export function HashtagPage(): JSX.Element {
  const { tag } = useParams<{ tag: string }>();
  const { pulses, isLoading, hasMore, loadMore } = useFeed(`/api/pulse/hashtag/${tag}`);

  return (
    <div className="flex min-h-screen bg-surface-page">
      <PulseNavigation />

      <main className="flex-1 border-x border-border max-w-2xl">
        <div className="sticky top-0 z-10 backdrop-blur-xl bg-surface-page/80 border-b border-border px-4 py-3">
          <h1 className="text-base font-semibold tracking-tight text-text-primary">
            #{tag}
          </h1>
        </div>

        <Feed
          pulses={pulses}
          isLoading={isLoading}
          hasMore={hasMore}
          onLoadMore={loadMore}
        />
      </main>

      <PulseRightSidebar />
    </div>
  );
}
```

---

## 3. Who to Follow - Status Confirmed

### ✅ **Already Fully Implemented**

After careful review, "Who to Follow" is **NOT a placeholder**. It's a complete, production-ready feature with:

- **Backend**: 2-hop mutual follow algorithm in `GetSuggestedProfiles.cs`
- **Repository**: Optimized single-query implementation using EF Core joins
- **Frontend**: Fully functional `WhoToFollow.tsx` component with `useSuggestedProfiles` hook
- **API**: `GET /api/social/suggestions` endpoint

**Algorithm**: Suggests users who are followed by people you follow (2-hop network), ranked by mutual follow count.

**No work needed.**

---

## 4. Onboarding Flow

### **Goal**
Guide new users through profile completion, first pulse, and suggested follows.

### **Database Schema**

> **⚠️ CRITICAL WARNING: DO NOT EXECUTE THESE SQL SCRIPTS**
>
> The SQL scripts below are **for documentation and reference purposes ONLY**.
>
> **ALL database schema changes MUST be implemented through:**
> 1. Update entity in `Codewrinkles.Domain/Identity/Profile.cs`
> 2. EF Core migration: `dotnet ef migrations add AddProfileOnboardingFlag`
> 3. Apply migration: `dotnet ef database update`
>
> **Executing raw SQL DDL statements (ALTER TABLE, CREATE INDEX, etc.) is strictly forbidden.**

#### Update `identity.Profiles` (Reference Only - Use EF Migrations)

```sql
ALTER TABLE identity.Profiles ADD OnboardingCompleted BIT NOT NULL DEFAULT 0;
```

### **Backend Implementation**

#### **Domain Layer - Update Profile**

**Update**: `apps/backend/src/Codewrinkles.Domain/Identity/Profile.cs`

```csharp
public bool OnboardingCompleted { get; private set; }

// In Create factory method:
OnboardingCompleted = false

// Add method:
public void CompleteOnboarding()
{
    OnboardingCompleted = true;
}
```

#### **Migration**

```bash
dotnet ef migrations add AddProfileOnboardingFlag --project ../Codewrinkles.Infrastructure
dotnet ef database update
```

#### **Application Layer - New Command**

**File**: `apps/backend/src/Codewrinkles.Application/Users/CompleteOnboarding.cs`

```csharp
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity.Exceptions;

namespace Codewrinkles.Application.Users;

public sealed record CompleteOnboardingCommand(Guid ProfileId) : ICommand<Unit>;

public sealed class CompleteOnboardingCommandHandler
    : ICommandHandler<CompleteOnboardingCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public CompleteOnboardingCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(
        CompleteOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByIdAsync(
            command.ProfileId,
            cancellationToken);

        if (profile == null)
            throw new ProfileNotFoundException(command.ProfileId);

        profile.CompleteOnboarding();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

**File**: `apps/backend/src/Codewrinkles.Application/Users/GetOnboardingStatus.cs`

```csharp
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Users;

public sealed record GetOnboardingStatusQuery(Guid ProfileId) : ICommand<OnboardingStatusResponse>;

public sealed record OnboardingStatusResponse(
    bool IsCompleted,
    bool HasHandle,
    bool HasBio,
    bool HasAvatar,
    bool HasPostedPulse,
    int FollowingCount);

public sealed class GetOnboardingStatusQueryHandler
    : ICommandHandler<GetOnboardingStatusQuery, OnboardingStatusResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOnboardingStatusQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OnboardingStatusResponse> HandleAsync(
        GetOnboardingStatusQuery query,
        CancellationToken cancellationToken)
    {
        var profile = await _unitOfWork.Profiles.FindByIdAsync(
            query.ProfileId,
            cancellationToken);

        if (profile == null)
        {
            return new OnboardingStatusResponse(
                IsCompleted: false,
                HasHandle: false,
                HasBio: false,
                HasAvatar: false,
                HasPostedPulse: false,
                FollowingCount: 0);
        }

        // Check if user has posted at least one pulse
        var pulseCount = await _unitOfWork.Pulses.GetPulseCountByAuthorAsync(
            profile.Id,
            cancellationToken);

        // Check following count
        var followingCount = await _unitOfWork.Follows.GetFollowingCountAsync(
            profile.Id,
            cancellationToken);

        return new OnboardingStatusResponse(
            IsCompleted: profile.OnboardingCompleted,
            HasHandle: !string.IsNullOrEmpty(profile.Handle),
            HasBio: !string.IsNullOrEmpty(profile.Bio),
            HasAvatar: !string.IsNullOrEmpty(profile.AvatarUrl),
            HasPostedPulse: pulseCount > 0,
            FollowingCount: followingCount);
    }
}
```

**Add to Repository**: `IPulseRepository.cs`

```csharp
Task<int> GetPulseCountByAuthorAsync(Guid authorId, CancellationToken cancellationToken);
```

**Implement in**: `PulseRepository.cs`

```csharp
public async Task<int> GetPulseCountByAuthorAsync(
    Guid authorId,
    CancellationToken cancellationToken)
{
    return await _pulses
        .Where(p => p.AuthorId == authorId && !p.IsDeleted)
        .CountAsync(cancellationToken);
}
```

**Add to Repository**: `IFollowRepository.cs`

```csharp
Task<int> GetFollowingCountAsync(Guid followerId, CancellationToken cancellationToken);
```

**Implement in**: `FollowRepository.cs`

```csharp
public async Task<int> GetFollowingCountAsync(
    Guid followerId,
    CancellationToken cancellationToken)
{
    return await _follows
        .Where(f => f.FollowerId == followerId)
        .CountAsync(cancellationToken);
}
```

#### **Application Layer - Get Popular Users**

**File**: `apps/backend/src/Codewrinkles.Application/Social/GetPopularProfiles.cs`

```csharp
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Social;

public sealed record GetPopularProfilesQuery(int Limit = 10) : ICommand<PopularProfilesResponse>;

public sealed record PopularProfilesResponse(IReadOnlyList<ProfileSuggestion> Profiles);

public sealed class GetPopularProfilesQueryHandler
    : ICommandHandler<GetPopularProfilesQuery, PopularProfilesResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPopularProfilesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PopularProfilesResponse> HandleAsync(
        GetPopularProfilesQuery query,
        CancellationToken cancellationToken)
    {
        var popular = await _unitOfWork.Profiles.GetMostFollowedProfilesAsync(
            query.Limit,
            cancellationToken);

        var dtos = popular.Select(p => new ProfileSuggestion(
            ProfileId: p.Id,
            Name: p.Name,
            Handle: p.Handle ?? string.Empty,
            AvatarUrl: p.AvatarUrl,
            Bio: p.Bio,
            MutualFollowCount: 0 // Not applicable for popular profiles
        )).ToList();

        return new PopularProfilesResponse(dtos);
    }
}
```

**Add to Repository**: `IProfileRepository.cs`

```csharp
Task<IReadOnlyList<Profile>> GetMostFollowedProfilesAsync(int limit, CancellationToken cancellationToken);
```

**Implement in**: `ProfileRepository.cs`

```csharp
public async Task<IReadOnlyList<Profile>> GetMostFollowedProfilesAsync(
    int limit,
    CancellationToken cancellationToken)
{
    // Profiles with most followers
    return await _profiles
        .OrderByDescending(p => p.FollowerCount)
        .Take(limit)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}
```

#### **API Endpoints**

**Update**: `apps/backend/src/Codewrinkles.API/Modules/Identity/IdentityEndpoints.cs`

```csharp
// GET /api/profile/onboarding/status
group.MapGet("/onboarding/status", async (
    [FromServices] ICommander commander,
    HttpContext httpContext,
    CancellationToken cancellationToken = default) =>
{
    var profileId = httpContext.User.GetProfileId();
    var query = new GetOnboardingStatusQuery(profileId);
    var result = await commander.ExecuteAsync(query, cancellationToken);
    return Results.Ok(result);
})
.RequireAuthorization()
.WithName("GetOnboardingStatus")
.WithTags("Identity");

// POST /api/profile/onboarding/complete
group.MapPost("/onboarding/complete", async (
    [FromServices] ICommander commander,
    HttpContext httpContext,
    CancellationToken cancellationToken = default) =>
{
    var profileId = httpContext.User.GetProfileId();
    var command = new CompleteOnboardingCommand(profileId);
    await commander.ExecuteAsync(command, cancellationToken);
    return Results.NoContent();
})
.RequireAuthorization()
.WithName("CompleteOnboarding")
.WithTags("Identity");
```

**Update**: `apps/backend/src/Codewrinkles.API/Modules/Social/SocialEndpoints.cs`

```csharp
// GET /api/social/popular
group.MapGet("/popular", async (
    [FromServices] ICommander commander,
    [FromQuery] int limit = 10,
    CancellationToken cancellationToken = default) =>
{
    var query = new GetPopularProfilesQuery(limit);
    var result = await commander.ExecuteAsync(query, cancellationToken);
    return Results.Ok(result);
})
.WithName("GetPopularProfiles")
.WithTags("Social");
```

### **Frontend Implementation**

**New Page**: `apps/frontend/src/features/onboarding/OnboardingFlow.tsx`

```typescript
import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { CompleteProfile } from "./steps/CompleteProfile";
import { FirstPulse } from "./steps/FirstPulse";
import { SuggestedFollows } from "./steps/SuggestedFollows";
import { useAuth } from "../../hooks/useAuth";

type OnboardingStep = "profile" | "pulse" | "follows";

interface OnboardingStatus {
  isCompleted: boolean;
  hasHandle: boolean;
  hasBio: boolean;
  hasAvatar: boolean;
  hasPostedPulse: boolean;
  followingCount: number;
}

export function OnboardingFlow(): JSX.Element {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [status, setStatus] = useState<OnboardingStatus | null>(null);
  const [currentStep, setCurrentStep] = useState<OnboardingStep>("profile");

  useEffect(() => {
    async function fetchStatus() {
      const response = await fetch('/api/profile/onboarding/status');
      const data: OnboardingStatus = await response.json();
      setStatus(data);

      // Redirect if already completed
      if (data.isCompleted) {
        navigate('/social');
        return;
      }

      // Determine starting step
      if (!data.hasHandle || !data.hasBio) {
        setCurrentStep("profile");
      } else if (!data.hasPostedPulse) {
        setCurrentStep("pulse");
      } else {
        setCurrentStep("follows");
      }
    }

    void fetchStatus();
  }, [navigate]);

  const handleProfileComplete = (): void => {
    setCurrentStep("pulse");
  };

  const handlePulseComplete = (): void => {
    setCurrentStep("follows");
  };

  const handleFollowsComplete = async (): Promise<void> => {
    await fetch('/api/profile/onboarding/complete', { method: 'POST' });
    navigate('/social');
  };

  if (!status) {
    return <div>Loading...</div>;
  }

  return (
    <div className="min-h-screen bg-surface-page flex items-center justify-center p-4">
      <div className="max-w-2xl w-full">
        {/* Progress Indicator */}
        <div className="mb-8">
          <div className="flex items-center justify-between mb-2">
            <div className={`flex items-center ${currentStep === "profile" ? "text-brand" : "text-text-tertiary"}`}>
              <span className="text-sm font-medium">1. Profile</span>
            </div>
            <div className={`flex items-center ${currentStep === "pulse" ? "text-brand" : "text-text-tertiary"}`}>
              <span className="text-sm font-medium">2. First Pulse</span>
            </div>
            <div className={`flex items-center ${currentStep === "follows" ? "text-brand" : "text-text-tertiary"}`}>
              <span className="text-sm font-medium">3. Follow People</span>
            </div>
          </div>
          <div className="h-1 bg-surface-card1 rounded-full overflow-hidden">
            <div
              className="h-full bg-brand transition-all duration-300"
              style={{
                width: currentStep === "profile" ? "33%" : currentStep === "pulse" ? "66%" : "100%"
              }}
            />
          </div>
        </div>

        {/* Steps */}
        {currentStep === "profile" && <CompleteProfile onComplete={handleProfileComplete} />}
        {currentStep === "pulse" && <FirstPulse onComplete={handlePulseComplete} />}
        {currentStep === "follows" && <SuggestedFollows onComplete={handleFollowsComplete} />}
      </div>
    </div>
  );
}
```

**Step 1**: `apps/frontend/src/features/onboarding/steps/CompleteProfile.tsx`

```typescript
import { useState } from "react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";
import { useAuth } from "../../../hooks/useAuth";

interface CompleteProfileProps {
  onComplete: () => void;
}

export function CompleteProfile({ onComplete }: CompleteProfileProps): JSX.Element {
  const { user } = useAuth();
  const [handle, setHandle] = useState(user?.handle || "");
  const [bio, setBio] = useState(user?.bio || "");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      // Call update profile endpoint
      await fetch(`/api/profile/${user?.profileId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ handle, bio })
      });

      onComplete();
    } catch (error) {
      console.error('Failed to update profile:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Card>
      <h2 className="text-xl font-bold text-text-primary mb-2">
        Complete Your Profile
      </h2>
      <p className="text-sm text-text-secondary mb-6">
        Let people know who you are
      </p>

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-text-primary mb-2">
            Handle
          </label>
          <div className="relative">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary">
              @
            </span>
            <input
              type="text"
              value={handle}
              onChange={(e) => setHandle(e.target.value)}
              className="w-full pl-8 pr-3 py-2 bg-surface-card2 border border-border rounded-lg text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-brand"
              placeholder="yourhandle"
              required
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-text-primary mb-2">
            Bio
          </label>
          <textarea
            value={bio}
            onChange={(e) => setBio(e.target.value)}
            rows={3}
            className="w-full px-3 py-2 bg-surface-card2 border border-border rounded-lg text-sm text-text-primary focus:outline-none focus:ring-2 focus:ring-brand resize-none"
            placeholder="Tell us about yourself..."
            required
          />
        </div>

        <Button type="submit" variant="primary" disabled={isSubmitting}>
          {isSubmitting ? "Saving..." : "Continue"}
        </Button>
      </form>
    </Card>
  );
}
```

**Step 2**: `apps/frontend/src/features/onboarding/steps/FirstPulse.tsx`

```typescript
import { useState } from "react";
import { Card } from "../../../components/ui/Card";
import { UnifiedComposer } from "../../pulse/UnifiedComposer";

interface FirstPulseProps {
  onComplete: () => void;
}

export function FirstPulse({ onComplete }: FirstPulseProps): JSX.Element {
  const handlePulseCreated = (): void => {
    onComplete();
  };

  return (
    <Card>
      <h2 className="text-xl font-bold text-text-primary mb-2">
        Share Your First Pulse
      </h2>
      <p className="text-sm text-text-secondary mb-6">
        Introduce yourself to the Codewrinkles community
      </p>

      <UnifiedComposer
        onPulseCreated={handlePulseCreated}
        placeholder="Hey everyone! I'm new to Codewrinkles..."
      />
    </Card>
  );
}
```

**Step 3**: `apps/frontend/src/features/onboarding/steps/SuggestedFollows.tsx`

```typescript
import { useState, useEffect } from "react";
import { Card } from "../../../components/ui/Card";
import { Button } from "../../../components/ui/Button";
import { FollowButton } from "../../social/components/FollowButton";
import type { ProfileSuggestion } from "../../../types";

interface SuggestedFollowsProps {
  onComplete: () => void;
}

export function SuggestedFollows({ onComplete }: SuggestedFollowsProps): JSX.Element {
  const [profiles, setProfiles] = useState<ProfileSuggestion[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function fetchPopular() {
      try {
        const response = await fetch('/api/social/popular?limit=10');
        const data = await response.json();
        setProfiles(data.profiles);
      } catch (error) {
        console.error('Failed to fetch popular profiles:', error);
      } finally {
        setIsLoading(false);
      }
    }

    void fetchPopular();
  }, []);

  return (
    <Card>
      <h2 className="text-xl font-bold text-text-primary mb-2">
        Follow People You're Interested In
      </h2>
      <p className="text-sm text-text-secondary mb-6">
        Start building your network (you can skip this step)
      </p>

      {isLoading ? (
        <p className="text-sm text-text-tertiary">Loading suggestions...</p>
      ) : (
        <div className="space-y-4 mb-6">
          {profiles.map((profile) => (
            <div key={profile.profileId} className="flex items-start gap-3 p-3 bg-surface-card2 rounded-lg">
              <div className="h-12 w-12 rounded-full bg-surface-card1 flex items-center justify-center text-lg font-semibold">
                {profile.name.charAt(0)}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-text-primary truncate">
                  {profile.name}
                </p>
                <p className="text-xs text-text-tertiary truncate">
                  @{profile.handle}
                </p>
                {profile.bio && (
                  <p className="text-xs text-text-secondary mt-1 line-clamp-2">
                    {profile.bio}
                  </p>
                )}
              </div>
              <FollowButton profileId={profile.profileId} size="sm" />
            </div>
          ))}
        </div>
      )}

      <div className="flex gap-3">
        <Button variant="secondary" onClick={onComplete}>
          Skip for Now
        </Button>
        <Button variant="primary" onClick={onComplete}>
          Continue to Pulse
        </Button>
      </div>
    </Card>
  );
}
```

**Add Route**: `apps/frontend/src/App.tsx`

```typescript
<Route path="/onboarding" element={<OnboardingFlow />} />
```

**Check onboarding on login**: Update login redirect logic to check `onboardingCompleted` flag.

---

## 5. Admin System & Dashboard

### **Goal**
Role-based admin access with dashboard showing key metrics for marketing screenshots.

### **Database Schema**

> **⚠️ CRITICAL WARNING: DO NOT EXECUTE THESE SQL SCRIPTS**
>
> The SQL scripts below are **for documentation and reference purposes ONLY**.
>
> **ALL database schema changes MUST be implemented through:**
> 1. Update entity in `Codewrinkles.Domain/Identity/Identity.cs`
> 2. EF Core migration: `dotnet ef migrations add AddUserRole`
> 3. Apply migration: `dotnet ef database update`
>
> **Executing raw SQL DDL statements (ALTER TABLE, CREATE INDEX, etc.) is strictly forbidden.**

#### Update `identity.Identities` (Reference Only - Use EF Migrations)

```sql
ALTER TABLE identity.Identities ADD Role VARCHAR(20) NOT NULL DEFAULT 'User';
CREATE INDEX IX_Identities_Role ON identity.Identities(Role);
```

### **Backend Implementation**

#### **Domain Layer - User Role**

**File**: `apps/backend/src/Codewrinkles.Domain/Identity/UserRole.cs`

```csharp
namespace Codewrinkles.Domain.Identity;

public enum UserRole : byte
{
    User = 0,
    Admin = 1
}
```

**Update**: `apps/backend/src/Codewrinkles.Domain/Identity/Identity.cs`

```csharp
public UserRole Role { get; private set; }

// In Create factory method:
Role = UserRole.User

// Add method:
public void PromoteToAdmin()
{
    Role = UserRole.Admin;
}
```

#### **Migration**

```bash
dotnet ef migrations add AddUserRole --project ../Codewrinkles.Infrastructure
dotnet ef database update
```

#### **Update JWT Claims**

**Update**: `apps/backend/src/Codewrinkles.Application/Users/LoginUser.cs`

```csharp
// In HandleAsync, add role claim to JWT:
new Claim(ClaimTypes.Role, identity.Role.ToString())
```

#### **Authorization Policy**

**Update**: `apps/backend/src/Codewrinkles.API/Program.cs`

```csharp
builder.Services.AddAuthorization(options =>
{
    // Existing policies...

    // Admin-only policy
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});
```

#### **Application Layer - Admin Queries**

**File**: `apps/backend/src/Codewrinkles.Application/Admin/GetDashboardMetrics.cs`

```csharp
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;

namespace Codewrinkles.Application.Admin;

public sealed record GetDashboardMetricsQuery() : ICommand<DashboardMetricsResponse>;

public sealed record DashboardMetricsResponse(
    int TotalUsers,
    int TotalPulses,
    int PulsesLast24Hours,
    int ActiveUsersLast24Hours,
    int TotalLikes,
    int TotalFollows,
    int PendingReports,
    DateTime LastUpdated);

public sealed class GetDashboardMetricsQueryHandler
    : ICommandHandler<GetDashboardMetricsQuery, DashboardMetricsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDashboardMetricsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardMetricsResponse> HandleAsync(
        GetDashboardMetricsQuery query,
        CancellationToken cancellationToken)
    {
        var yesterday = DateTime.UtcNow.AddDays(-1);

        // Run queries in parallel for performance
        var totalUsersTask = _unitOfWork.Profiles.GetTotalCountAsync(cancellationToken);
        var totalPulsesTask = _unitOfWork.Pulses.GetTotalCountAsync(cancellationToken);
        var pulsesLast24hTask = _unitOfWork.Pulses.GetCountSinceAsync(yesterday, cancellationToken);
        var activeUsersLast24hTask = _unitOfWork.Profiles.GetActiveUserCountSinceAsync(yesterday, cancellationToken);
        var totalLikesTask = _unitOfWork.Pulses.GetTotalLikeCountAsync(cancellationToken);
        var totalFollowsTask = _unitOfWork.Follows.GetTotalCountAsync(cancellationToken);
        var pendingReportsTask = _unitOfWork.Reports.GetPendingCountAsync(cancellationToken);

        await Task.WhenAll(
            totalUsersTask,
            totalPulsesTask,
            pulsesLast24hTask,
            activeUsersLast24hTask,
            totalLikesTask,
            totalFollowsTask,
            pendingReportsTask);

        return new DashboardMetricsResponse(
            TotalUsers: await totalUsersTask,
            TotalPulses: await totalPulsesTask,
            PulsesLast24Hours: await pulsesLast24hTask,
            ActiveUsersLast24Hours: await activeUsersLast24hTask,
            TotalLikes: await totalLikesTask,
            TotalFollows: await totalFollowsTask,
            PendingReports: await pendingReportsTask,
            LastUpdated: DateTime.UtcNow);
    }
}
```

**Add Repository Methods**: (Add these to respective repository interfaces and implementations)

```csharp
// IProfileRepository
Task<int> GetTotalCountAsync(CancellationToken cancellationToken);
Task<int> GetActiveUserCountSinceAsync(DateTime since, CancellationToken cancellationToken);

// IPulseRepository
Task<int> GetTotalCountAsync(CancellationToken cancellationToken);
Task<int> GetCountSinceAsync(DateTime since, CancellationToken cancellationToken);
Task<int> GetTotalLikeCountAsync(CancellationToken cancellationToken);

// IFollowRepository
Task<int> GetTotalCountAsync(CancellationToken cancellationToken);

// IReportRepository (to be created)
Task<int> GetPendingCountAsync(CancellationToken cancellationToken);
```

#### **API Endpoint**

**File**: `apps/backend/src/Codewrinkles.API/Modules/Admin/AdminEndpoints.cs`

```csharp
using Kommand.Abstractions;
using Codewrinkles.Application.Admin;
using Microsoft.AspNetCore.Mvc;

namespace Codewrinkles.API.Modules.Admin;

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this RouteGroupBuilder group)
    {
        // GET /api/admin/dashboard/metrics
        group.MapGet("/dashboard/metrics", async (
            [FromServices] ICommander commander,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetDashboardMetricsQuery();
            var result = await commander.ExecuteAsync(query, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("GetDashboardMetrics")
        .WithTags("Admin");

        return group;
    }
}
```

**Register in**: `Program.cs`

```csharp
var adminGroup = app.MapGroup("/api/admin");
adminGroup.MapAdminEndpoints();
```

### **Frontend Implementation**

> **🎨 Design Philosophy**: Admin is a first-class app in the ecosystem. It should seamlessly fit the existing UI patterns (like Pulse) with left navigation, full-width layout, and consistent styling.

#### **1. Update App Switcher to Show Admin (Conditional)**

**Update**: `apps/frontend/src/features/shell/AppSwitcher.tsx`

```typescript
import { useState } from "react";
import { Link } from "react-router-dom";
import type { App } from "../../types";
import { useAuth } from "../../hooks/useAuth";

interface AppWithPath extends App {
  path?: string;
  adminOnly?: boolean;
}

const APPS: AppWithPath[] = [
  { id: "social", name: "Social", accent: "sky", description: "Micro-thought stream", path: "/social" },
  { id: "learn", name: "Learn", accent: "violet", description: "Guided learning paths", path: "/learn" },
  { id: "twin", name: "Twin", accent: "brand", description: "Your knowledge twin", path: "/twin" },
  { id: "admin", name: "Admin", accent: "amber", description: "System administration", path: "/admin", adminOnly: true }, // NEW
  { id: "legal", name: "Legal", accent: "amber", description: "Contracts (soon)" },
  { id: "runwrinkles", name: "Runwrinkles", accent: "emerald", description: "Running coach (soon)" },
];

function getAccentClass(accent: App["accent"]): string {
  const accentClasses = {
    sky: "bg-sky-400",
    violet: "bg-violet-400",
    brand: "bg-brand-soft",
    amber: "bg-amber-400",
    emerald: "bg-emerald-400",
  };
  return accentClasses[accent];
}

export function AppSwitcher(): JSX.Element {
  const [isOpen, setIsOpen] = useState(false);
  const { user } = useAuth();

  // Filter apps based on admin status
  const visibleApps = APPS.filter(app => {
    if (app.adminOnly) {
      return user?.role === "Admin";
    }
    return true;
  });

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="hidden sm:flex items-center gap-2 rounded-full border border-border bg-surface-card1 px-3 py-1 text-xs text-text-secondary hover:border-brand-soft/60 hover:bg-surface-card2 transition-all duration-150"
      >
        <span className="inline-flex h-4 w-4 items-center justify-center rounded-full bg-surface-page text-[10px] text-brand-soft">
          ⬢
        </span>
        <span>Apps</span>
        <span className="text-[9px] opacity-70">{isOpen ? "▲" : "▼"}</span>
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-64 rounded-2xl border border-border bg-surface-card1 shadow-sm p-3 z-20 animate-fadeIn">
          <p className="mb-2 text-[11px] text-text-tertiary">
            Switch between apps in your Codewrinkles universe.
          </p>
          <div className="space-y-1">
            {visibleApps.map((app) => {
              const Component = app.path ? Link : "div";
              const clickHandler = app.path ? () => setIsOpen(false) : undefined;
              return (
                <Component
                  key={app.id}
                  to={app.path || ""}
                  onClick={clickHandler}
                  className="w-full flex items-start justify-between gap-2 rounded-xl px-2.5 py-2 text-left text-xs text-text-secondary hover:bg-surface-card2 transition-colors cursor-pointer"
                >
                  <div>
                    <div className="flex items-center gap-2">
                      <span className={`h-1.5 w-1.5 rounded-full ${getAccentClass(app.accent)}`}></span>
                      <span className="text-text-primary">{app.name}</span>
                    </div>
                    <span className="mt-0.5 block text-[11px] text-text-tertiary">
                      {app.description}
                    </span>
                  </div>
                  {(app.id === "legal" || app.id === "runwrinkles") && (
                    <span className="rounded-full border border-border px-1.5 py-[1px] text-[9px] text-text-tertiary">
                      Soon
                    </span>
                  )}
                </Component>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
```

#### **2. Create Admin Navigation (Left Sidebar)**

**File**: `apps/frontend/src/features/admin/AdminNavigation.tsx`

```typescript
import { NavLink } from "react-router-dom";

interface NavItemProps {
  to: string;
  icon: string;
  label: string;
  badge?: number;
}

function NavItem({ to, icon, label, badge }: NavItemProps): JSX.Element {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        `flex items-center gap-3 rounded-full px-4 py-3 text-sm font-medium transition-colors ${
          isActive
            ? "bg-surface-card2 text-text-primary"
            : "text-text-secondary hover:bg-surface-card1 hover:text-text-primary"
        }`
      }
    >
      <span className="text-lg w-6 text-center">{icon}</span>
      <span className="hidden xl:inline">{label}</span>
      {badge !== undefined && badge > 0 && (
        <span className="ml-auto hidden xl:inline rounded-full bg-brand-soft px-2 py-0.5 text-[10px] font-semibold text-black">
          {badge > 99 ? "99+" : badge}
        </span>
      )}
    </NavLink>
  );
}

export function AdminNavigation(): JSX.Element {
  return (
    <nav className="sticky top-20 z-10 space-y-1">
      <NavItem to="/admin" icon="📊" label="Dashboard" />
      <NavItem to="/admin/reports" icon="🚨" label="Reports" />
      <NavItem to="/admin/users" icon="👥" label="Users" />
      <NavItem to="/admin/settings" icon="⚙️" label="Settings" />
    </nav>
  );
}
```

#### **3. Create Admin Layout (Main Page)**

**File**: `apps/frontend/src/features/admin/AdminPage.tsx`

```typescript
import { Outlet, Navigate } from "react-router-dom";
import { AdminNavigation } from "./AdminNavigation";
import { useAuth } from "../../hooks/useAuth";

export function AdminPage(): JSX.Element {
  const { user } = useAuth();

  // Redirect non-admin users
  if (user?.role !== "Admin") {
    return <Navigate to="/social" replace />;
  }

  return (
    <div className="flex justify-center">
      {/* Left Navigation */}
      <aside className="hidden lg:flex w-[320px] flex-shrink-0 justify-end pr-8">
        <div className="w-[240px]">
          <AdminNavigation />
        </div>
      </aside>

      {/* Main Content - Full width (no right sidebar) */}
      <main className="w-full max-w-[1200px] min-h-screen">
        <Outlet />
      </main>
    </div>
  );
}
```

#### **4. Create Dashboard Page**

**File**: `apps/frontend/src/features/admin/DashboardPage.tsx`

```typescript
import { useEffect, useState } from "react";
import { Card } from "../../components/ui/Card";
import { Link } from "react-router-dom";

interface DashboardMetrics {
  totalUsers: number;
  totalPulses: number;
  pulsesLast24Hours: number;
  activeUsersLast24Hours: number;
  totalLikes: number;
  totalFollows: number;
  pendingReports: number;
  lastUpdated: string;
}

export function DashboardPage(): JSX.Element {
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    async function fetchMetrics() {
      try {
        const response = await fetch('/api/admin/dashboard/metrics');
        const data: DashboardMetrics = await response.json();
        setMetrics(data);
      } catch (error) {
        console.error('Failed to fetch metrics:', error);
      } finally {
        setIsLoading(false);
      }
    }

    void fetchMetrics();
  }, []);

  if (isLoading || !metrics) {
    return (
      <div className="p-8">
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-surface-card1 rounded w-48"></div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {Array.from({ length: 7 }).map((_, i) => (
              <div key={i} className="h-32 bg-surface-card1 rounded-xl"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="p-8">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-text-primary mb-1">
          Dashboard
        </h1>
        <p className="text-sm text-text-secondary">
          System metrics and overview
        </p>
      </div>

      {/* Metrics Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <MetricCard
          title="Total Users"
          value={metrics.totalUsers}
          icon="👥"
          to="/admin/users"
        />
        <MetricCard
          title="Total Pulses"
          value={metrics.totalPulses}
          icon="💬"
        />
        <MetricCard
          title="Pulses (24h)"
          value={metrics.pulsesLast24Hours}
          icon="📈"
        />
        <MetricCard
          title="Active Users (24h)"
          value={metrics.activeUsersLast24Hours}
          icon="⚡"
        />
        <MetricCard
          title="Total Likes"
          value={metrics.totalLikes}
          icon="❤️"
        />
        <MetricCard
          title="Total Follows"
          value={metrics.totalFollows}
          icon="🔗"
        />
        <MetricCard
          title="Pending Reports"
          value={metrics.pendingReports}
          icon="🚨"
          highlight={metrics.pendingReports > 0}
          to="/admin/reports"
        />
      </div>

      {/* Last Updated */}
      <p className="text-xs text-text-tertiary text-center">
        Last updated: {new Date(metrics.lastUpdated).toLocaleString()}
      </p>
    </div>
  );
}

interface MetricCardProps {
  title: string;
  value: number;
  icon: string;
  highlight?: boolean;
  to?: string;
}

function MetricCard({ title, value, icon, highlight, to }: MetricCardProps): JSX.Element {
  const content = (
    <>
      <div className="flex items-center justify-between mb-2">
        <span className="text-sm font-medium text-text-secondary">{title}</span>
        <span className="text-2xl">{icon}</span>
      </div>
      <p className="text-3xl font-bold text-text-primary">
        {value.toLocaleString()}
      </p>
    </>
  );

  if (to) {
    return (
      <Link to={to}>
        <Card className={`cursor-pointer hover:border-brand-soft/40 transition-colors ${highlight ? "border-2 border-brand" : ""}`}>
          {content}
        </Card>
      </Link>
    );
  }

  return (
    <Card className={highlight ? "border-2 border-brand" : ""}>
      {content}
    </Card>
  );
}
```

#### **5. Update Routing**

**Update**: `apps/frontend/src/App.tsx`

```typescript
import { AdminPage } from "./features/admin/AdminPage";
import { DashboardPage } from "./features/admin/DashboardPage";
import { ReportsPage } from "./features/admin/ReportsPage"; // Will create in Session 4

// Add nested admin routes
<Route path="/admin" element={<AdminPage />}>
  <Route index element={<DashboardPage />} />
  <Route path="reports" element={<ReportsPage />} />
  {/* Future: /admin/users, /admin/settings */}
</Route>
```

#### **6. Update User Type to Include Role**

**Update**: `apps/frontend/src/types.ts`

```typescript
export interface User {
  id: string;
  email: string;
  name: string;
  handle?: string;
  avatarUrl?: string;
  profileId: string;
  role?: "User" | "Admin"; // Add role property
}
```

#### **7. Create Index File for Clean Imports**

**File**: `apps/frontend/src/features/admin/index.ts`

```typescript
export { AdminPage } from "./AdminPage";
export { AdminNavigation } from "./AdminNavigation";
export { DashboardPage } from "./DashboardPage";
```

---

## 6. Pulse Reporting System

> **⏭️ DEFERRED FEATURE - NOT IMPLEMENTED FOR MVP**
>
> This section is kept for future reference. The pulse reporting system is **intentionally skipped** for the invitation-only MVP launch.
>
> **Why**: With a small, curated community, manual moderation via admin dashboard is sufficient.
>
> **When to implement**: If community grows beyond ~100-200 active users or transitions to public registration.

### **Goal**
Users can report inappropriate pulses. Admins can review and take action.

### **Database Schema**

> **⚠️ CRITICAL WARNING: DO NOT EXECUTE THESE SQL SCRIPTS**
>
> The SQL scripts below are **for documentation and reference purposes ONLY**.
>
> **ALL database schema changes MUST be implemented through:**
> 1. Entity classes in `Codewrinkles.Domain/Pulse/`
> 2. EF Core configurations in `Codewrinkles.Infrastructure/Persistence/Configurations/Pulse/`
> 3. EF Core migrations: `dotnet ef migrations add <MigrationName>`
> 4. Apply migrations: `dotnet ef database update`
>
> **Executing raw SQL DDL statements is strictly forbidden.**
>
> These scripts exist only to visualize the target schema structure.

#### `pulse.PulseReports` (Reference Only - Use EF Migrations)

```sql
CREATE TABLE pulse.PulseReports (
    Id              UNIQUEIDENTIFIER    PRIMARY KEY,
    PulseId         UNIQUEIDENTIFIER    NOT NULL,   -- FK → pulse.Pulses
    ReporterId      UNIQUEIDENTIFIER    NOT NULL,   -- FK → identity.Profiles
    Reason          NVARCHAR(50)        NOT NULL,   -- Enum: Spam, Harassment, etc.
    Description     NVARCHAR(500)       NULL,
    Status          NVARCHAR(20)        NOT NULL DEFAULT 'Pending',
    ReviewedAt      DATETIME2           NULL,
    ReviewedBy      UNIQUEIDENTIFIER    NULL,       -- FK → identity.Profiles (admin)
    AdminNotes      NVARCHAR(1000)      NULL,
    CreatedAt       DATETIME2           NOT NULL,

    CONSTRAINT FK_PulseReports_Pulse FOREIGN KEY (PulseId)
        REFERENCES pulse.Pulses(Id),
    CONSTRAINT FK_PulseReports_Reporter FOREIGN KEY (ReporterId)
        REFERENCES identity.Profiles(Id),
    CONSTRAINT FK_PulseReports_Reviewer FOREIGN KEY (ReviewedBy)
        REFERENCES identity.Profiles(Id)
);

CREATE INDEX IX_PulseReports_Status ON pulse.PulseReports(Status);
CREATE INDEX IX_PulseReports_PulseId ON pulse.PulseReports(PulseId);
CREATE INDEX IX_PulseReports_CreatedAt ON pulse.PulseReports(CreatedAt DESC);
```

### **Backend Implementation**

#### **Domain Layer**

**File**: `apps/backend/src/Codewrinkles.Domain/Pulse/ReportReason.cs`

```csharp
namespace Codewrinkles.Domain.Pulse;

public enum ReportReason : byte
{
    Spam = 0,
    Harassment = 1,
    HateSpeech = 2,
    Violence = 3,
    Misinformation = 4,
    SexualContent = 5,
    Other = 6
}
```

**File**: `apps/backend/src/Codewrinkles.Domain/Pulse/ReportStatus.cs`

```csharp
namespace Codewrinkles.Domain.Pulse;

public enum ReportStatus : byte
{
    Pending = 0,
    UnderReview = 1,
    Resolved = 2,
    Dismissed = 3
}
```

**File**: `apps/backend/src/Codewrinkles.Domain/Pulse/PulseReport.cs`

```csharp
namespace Codewrinkles.Domain.Pulse;

public sealed class PulseReport
{
#pragma warning disable CS8618
    private PulseReport() { } // EF Core constructor
#pragma warning restore CS8618

    public Guid Id { get; private set; }
    public Guid PulseId { get; private set; }
    public Guid ReporterId { get; private set; }
    public ReportReason Reason { get; private set; }
    public string? Description { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public string? AdminNotes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Pulse Pulse { get; private set; }
    public Profile Reporter { get; private set; }
    public Profile? Reviewer { get; private set; }

    public static PulseReport Create(
        Guid pulseId,
        Guid reporterId,
        ReportReason reason,
        string? description = null)
    {
        return new PulseReport
        {
            // Id generated by EF Core
            PulseId = pulseId,
            ReporterId = reporterId,
            Reason = reason,
            Description = description?.Trim(),
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsUnderReview()
    {
        Status = ReportStatus.UnderReview;
    }

    public void Resolve(Guid reviewerId, string? adminNotes = null)
    {
        Status = ReportStatus.Resolved;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        AdminNotes = adminNotes?.Trim();
    }

    public void Dismiss(Guid reviewerId, string? adminNotes = null)
    {
        Status = ReportStatus.Dismissed;
        ReviewedBy = reviewerId;
        ReviewedAt = DateTime.UtcNow;
        AdminNotes = adminNotes?.Trim();
    }
}
```

#### **Application Layer**

**File**: `apps/backend/src/Codewrinkles.Application/Pulse/ReportPulse.cs`

```csharp
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Pulse;

public sealed record ReportPulseCommand(
    Guid PulseId,
    Guid ReporterId,
    ReportReason Reason,
    string? Description
) : ICommand<Unit>;

public sealed class ReportPulseValidator : IValidator<ReportPulseCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportPulseValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        ReportPulseCommand command,
        CancellationToken cancellationToken)
    {
        // Validate pulse exists
        var pulse = await _unitOfWork.Pulses.FindByIdAsync(
            command.PulseId,
            cancellationToken);

        if (pulse == null)
            throw new PulseNotFoundException(command.PulseId);

        return ValidationResult.Success();
    }
}

public sealed class ReportPulseCommandHandler
    : ICommandHandler<ReportPulseCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportPulseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(
        ReportPulseCommand command,
        CancellationToken cancellationToken)
    {
        var report = PulseReport.Create(
            pulseId: command.PulseId,
            reporterId: command.ReporterId,
            reason: command.Reason,
            description: command.Description);

        _unitOfWork.Reports.Create(report);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

**File**: `apps/backend/src/Codewrinkles.Application/Admin/GetPulseReports.cs`

```csharp
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Admin;

public sealed record GetPulseReportsQuery(
    ReportStatus? Status,
    string? Cursor,
    int Limit = 20
) : ICommand<PulseReportsResponse>;

public sealed record PulseReportDto(
    Guid Id,
    Guid PulseId,
    string PulseContent,
    string PulseAuthorName,
    Guid ReporterId,
    string ReporterName,
    string Reason,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? ReviewerName,
    string? AdminNotes);

public sealed record PulseReportsResponse(
    IReadOnlyList<PulseReportDto> Reports,
    string? NextCursor,
    bool HasMore);

public sealed class GetPulseReportsQueryHandler
    : ICommandHandler<GetPulseReportsQuery, PulseReportsResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPulseReportsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PulseReportsResponse> HandleAsync(
        GetPulseReportsQuery query,
        CancellationToken cancellationToken)
    {
        var (reports, nextCursor, hasMore) = await _unitOfWork.Reports.GetReportsAsync(
            query.Status,
            query.Cursor,
            query.Limit,
            cancellationToken);

        var dtos = reports.Select(r => new PulseReportDto(
            Id: r.Id,
            PulseId: r.PulseId,
            PulseContent: r.Pulse.Content,
            PulseAuthorName: r.Pulse.Author.Name,
            ReporterId: r.ReporterId,
            ReporterName: r.Reporter.Name,
            Reason: r.Reason.ToString(),
            Description: r.Description,
            Status: r.Status.ToString(),
            CreatedAt: r.CreatedAt,
            ReviewedAt: r.ReviewedAt,
            ReviewerName: r.Reviewer?.Name,
            AdminNotes: r.AdminNotes
        )).ToList();

        return new PulseReportsResponse(dtos, nextCursor, hasMore);
    }
}
```

**File**: `apps/backend/src/Codewrinkles.Application/Admin/ReviewPulseReport.cs`

```csharp
using Kommand.Abstractions;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Admin;

public sealed record ReviewPulseReportCommand(
    Guid ReportId,
    Guid ReviewerId,
    ReportStatus NewStatus,  // Resolved or Dismissed
    string? AdminNotes,
    bool DeletePulse = false
) : ICommand<Unit>;

public sealed class ReviewPulseReportCommandHandler
    : ICommandHandler<ReviewPulseReportCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewPulseReportCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> HandleAsync(
        ReviewPulseReportCommand command,
        CancellationToken cancellationToken)
    {
        var report = await _unitOfWork.Reports.FindByIdAsync(
            command.ReportId,
            cancellationToken);

        if (report == null)
            throw new InvalidOperationException("Report not found");

        // Update report status
        if (command.NewStatus == ReportStatus.Resolved)
        {
            report.Resolve(command.ReviewerId, command.AdminNotes);

            // Optionally delete the reported pulse
            if (command.DeletePulse)
            {
                var pulse = await _unitOfWork.Pulses.FindByIdAsync(
                    report.PulseId,
                    cancellationToken);

                pulse?.MarkAsDeleted();
            }
        }
        else if (command.NewStatus == ReportStatus.Dismissed)
        {
            report.Dismiss(command.ReviewerId, command.AdminNotes);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

#### **Repository Interface**

**File**: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IReportRepository.cs`

```csharp
using Codewrinkles.Domain.Pulse;

namespace Codewrinkles.Application.Common.Interfaces;

public interface IReportRepository
{
    void Create(PulseReport report);
    Task<PulseReport?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken);
    Task<(IReadOnlyList<PulseReport> Reports, string? NextCursor, bool HasMore)> GetReportsAsync(
        ReportStatus? status,
        string? cursor,
        int limit,
        CancellationToken cancellationToken);
}
```

**Add to UnitOfWork**:

```csharp
public interface IUnitOfWork
{
    // ... existing repositories
    IReportRepository Reports { get; }
}
```

#### **API Endpoints**

**Update**: `apps/backend/src/Codewrinkles.API/Modules/Pulse/PulseEndpoints.cs`

```csharp
// POST /api/pulse/{id}/report
group.MapPost("/{id:guid}/report", async (
    [FromRoute] Guid id,
    [FromBody] ReportPulseRequest request,
    [FromServices] ICommander commander,
    HttpContext httpContext,
    CancellationToken cancellationToken = default) =>
{
    var reporterId = httpContext.User.GetProfileId();

    var command = new ReportPulseCommand(
        PulseId: id,
        ReporterId: reporterId,
        Reason: request.Reason,
        Description: request.Description);

    await commander.ExecuteAsync(command, cancellationToken);

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("ReportPulse")
.WithTags("Pulse");
```

**Update**: `apps/backend/src/Codewrinkles.API/Modules/Admin/AdminEndpoints.cs`

```csharp
// GET /api/admin/reports
group.MapGet("/reports", async (
    [FromServices] ICommander commander,
    [FromQuery] string? status,
    [FromQuery] string? cursor,
    [FromQuery] int limit = 20,
    CancellationToken cancellationToken = default) =>
{
    ReportStatus? statusEnum = status != null
        ? Enum.Parse<ReportStatus>(status, ignoreCase: true)
        : null;

    var query = new GetPulseReportsQuery(statusEnum, cursor, limit);
    var result = await commander.ExecuteAsync(query, cancellationToken);
    return Results.Ok(result);
})
.RequireAuthorization("AdminOnly")
.WithName("GetPulseReports")
.WithTags("Admin");

// POST /api/admin/reports/{id}/review
group.MapPost("/reports/{id:guid}/review", async (
    [FromRoute] Guid id,
    [FromBody] ReviewReportRequest request,
    [FromServices] ICommander commander,
    HttpContext httpContext,
    CancellationToken cancellationToken = default) =>
{
    var reviewerId = httpContext.User.GetProfileId();

    var command = new ReviewPulseReportCommand(
        ReportId: id,
        ReviewerId: reviewerId,
        NewStatus: request.Status,
        AdminNotes: request.AdminNotes,
        DeletePulse: request.DeletePulse);

    await commander.ExecuteAsync(command, cancellationToken);

    return Results.NoContent();
})
.RequireAuthorization("AdminOnly")
.WithName("ReviewPulseReport")
.WithTags("Admin");
```

### **Frontend Implementation**

**Report Button**: Add to `PostCard.tsx` actions menu

```typescript
<button
  onClick={handleReport}
  className="flex items-center gap-2 px-3 py-2 text-sm text-text-secondary hover:bg-surface-card2 rounded"
>
  <span>🚨</span>
  <span>Report</span>
</button>
```

**Report Modal**: `apps/frontend/src/features/pulse/ReportModal.tsx`

```typescript
import { useState } from "react";
import { Button } from "../../components/ui/Button";

interface ReportModalProps {
  pulseId: string;
  onClose: () => void;
}

type ReportReason = "Spam" | "Harassment" | "HateSpeech" | "Violence" | "Misinformation" | "SexualContent" | "Other";

export function ReportModal({ pulseId, onClose }: ReportModalProps): JSX.Element {
  const [reason, setReason] = useState<ReportReason>("Spam");
  const [description, setDescription] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      await fetch(`/api/pulse/${pulseId}/report`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reason, description })
      });

      alert('Report submitted. Thank you for helping keep the community safe.');
      onClose();
    } catch (error) {
      console.error('Failed to submit report:', error);
      alert('Failed to submit report. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-surface-card1 rounded-xl p-6 max-w-md w-full mx-4">
        <h2 className="text-lg font-semibold text-text-primary mb-4">
          Report Pulse
        </h2>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-text-primary mb-2">
              Reason
            </label>
            <select
              value={reason}
              onChange={(e) => setReason(e.target.value as ReportReason)}
              className="w-full px-3 py-2 bg-surface-card2 border border-border rounded-lg text-sm text-text-primary"
            >
              <option value="Spam">Spam</option>
              <option value="Harassment">Harassment</option>
              <option value="HateSpeech">Hate Speech</option>
              <option value="Violence">Violence</option>
              <option value="Misinformation">Misinformation</option>
              <option value="SexualContent">Sexual Content</option>
              <option value="Other">Other</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-text-primary mb-2">
              Additional Details (Optional)
            </label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
              className="w-full px-3 py-2 bg-surface-card2 border border-border rounded-lg text-sm text-text-primary resize-none"
              placeholder="Provide more context about why you're reporting this..."
            />
          </div>

          <div className="flex gap-3">
            <Button type="button" variant="secondary" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" variant="primary" disabled={isSubmitting}>
              {isSubmitting ? "Submitting..." : "Submit Report"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
```

**Admin Reports Page**: `apps/frontend/src/features/admin/ReportsPage.tsx`

> **Note**: This page renders inside `AdminPage` layout (which includes left navigation and Outlet). It receives the full width of the main content area without additional wrappers.

```typescript
import { useEffect, useState } from "react";
import { Card } from "../../components/ui/Card";
import { Button } from "../../components/ui/Button";

interface PulseReport {
  id: string;
  pulseId: string;
  pulseContent: string;
  pulseAuthorName: string;
  reporterId: string;
  reporterName: string;
  reason: string;
  description: string | null;
  status: string;
  createdAt: string;
}

export function ReportsPage(): JSX.Element {
  const [reports, setReports] = useState<PulseReport[]>([]);
  const [filter, setFilter] = useState<string>("Pending");

  useEffect(() => {
    async function fetchReports() {
      const response = await fetch(`/api/admin/reports?status=${filter}`);
      const data = await response.json();
      setReports(data.reports);
    }

    void fetchReports();
  }, [filter]);

  const handleReview = async (
    reportId: string,
    status: "Resolved" | "Dismissed",
    deletePulse: boolean = false
  ): Promise<void> => {
    await fetch(`/api/admin/reports/${reportId}/review`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status, deletePulse })
    });

    // Refresh reports
    setReports(reports.filter(r => r.id !== reportId));
  };

  return (
    <div className="p-8">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-text-primary mb-1">
          Pulse Reports
        </h1>
        <p className="text-sm text-text-secondary">
          Review and moderate reported content
        </p>
      </div>

        {/* Filters */}
        <div className="flex gap-2 mb-6">
          {["Pending", "UnderReview", "Resolved", "Dismissed"].map((status) => (
            <button
              key={status}
              onClick={() => setFilter(status)}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                filter === status
                  ? "bg-brand text-white"
                  : "bg-surface-card1 text-text-secondary hover:bg-surface-card2"
              }`}
            >
              {status}
            </button>
          ))}
        </div>

        {/* Reports List */}
        <div className="space-y-4">
          {reports.map((report) => (
            <Card key={report.id}>
              <div className="space-y-3">
                <div>
                  <span className="text-xs font-medium text-brand uppercase">
                    {report.reason}
                  </span>
                  <p className="text-sm text-text-primary mt-1">
                    {report.pulseContent}
                  </p>
                  <p className="text-xs text-text-tertiary mt-1">
                    By @{report.pulseAuthorName}
                  </p>
                </div>

                {report.description && (
                  <div className="p-3 bg-surface-card2 rounded-lg">
                    <p className="text-xs text-text-secondary">
                      "{report.description}"
                    </p>
                    <p className="text-xs text-text-tertiary mt-1">
                      — Reported by @{report.reporterName}
                    </p>
                  </div>
                )}

                <div className="text-xs text-text-tertiary">
                  Reported {new Date(report.createdAt).toLocaleString()}
                </div>

                {report.status === "Pending" && (
                  <div className="flex gap-2">
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => handleReview(report.id, "Dismissed")}
                    >
                      Dismiss
                    </Button>
                    <Button
                      variant="primary"
                      size="sm"
                      onClick={() => handleReview(report.id, "Resolved", false)}
                    >
                      Resolve (Keep Pulse)
                    </Button>
                    <Button
                      variant="primary"
                      size="sm"
                      onClick={() => handleReview(report.id, "Resolved", true)}
                      className="bg-red-600 hover:bg-red-700"
                    >
                      Resolve & Delete Pulse
                    </Button>
                  </div>
                )}
              </div>
            </Card>
          ))}

          {reports.length === 0 && (
            <p className="text-center text-text-tertiary py-8">
              No {filter.toLowerCase()} reports
            </p>
          )}
        </div>
    </div>
  );
}
```

---

## Ready to Ship 🚀

### **MVP Launch Checklist**

All required features for invitation-only launch are **COMPLETE**:

- [x] **Link Preview System** - URLs automatically generate rich previews
- [x] **Hashtags & Popular Topics** - Hashtag extraction, trending sidebar, hashtag pages
- [x] **Admin System & Dashboard** - Role-based access, metrics dashboard
- [x] **Onboarding Flow** - 3-step wizard for new users
- [x] **Who to Follow** - Recommendation system based on follower overlap
- [x] **Core Pulse Features** - Create, like, re-pulse, reply, delete, bookmarks, mentions
- [x] **User Profiles** - Following/followers, profile editing, avatar upload
- [x] **Feed** - Chronological feed with cursor-based pagination

### **Pre-Launch Validation**

**Functional Testing**:
- [x] Link previews fetch correctly for multiple domains
- [x] Hashtags are extracted and trending topics update in real-time
- [x] Admin can access dashboard at `/admin`, non-admin users are blocked
- [x] Onboarding flow works for new users
- [x] Existing users (with `OnboardingCompleted = true`) skip onboarding
- [x] All migrations applied successfully
- [x] No TypeScript errors in frontend (`npm run lint`)
- [x] Backend builds without warnings (`dotnet build`)

**Manual Testing Workflow**:
1. Create new account → verify onboarding flow (profile → first pulse → follows)
2. Create pulses with URLs → verify link previews
3. Create pulses with #hashtags → verify trending sidebar updates
4. Click hashtag → verify filtered feed works
5. Test mobile responsive design
6. Login as admin → verify dashboard accessible
7. Create/like/re-pulse/reply/delete pulses → verify all interactions work
8. Test follow/unfollow → verify "Who to Follow" updates

### **Production Readiness**

**Database**:
- [x] All migrations applied to production database
- [x] At least one admin user promoted (manually set `Role = 'Admin'` in DB)

**Configuration**:
- [ ] Update `appsettings.Production.json` with production connection string
- [ ] Configure JWT secret in user secrets or environment variables
- [ ] Set CORS allowed origins for production frontend URL
- [ ] Configure file upload limits for avatars/images

**Deployment**:
- [ ] Backend deployed to hosting environment
- [ ] Frontend built and deployed (`npm run build`)
- [ ] Environment variables configured
- [ ] Database connection verified
- [ ] Health check endpoint responding

**Invitation System**:
- [ ] Create admin tool or SQL script to generate invitation codes
- [ ] Decide on invitation mechanism (codes, direct email invites, etc.)
- [ ] Document invitation process for yourself

### **Post-Launch Monitoring**

**What to Watch**:
- User registration and onboarding completion rate
- Pulse creation volume
- Hashtag usage and trending topics
- Link preview success rate (check logs for failures)
- Admin dashboard metrics (daily active users, total pulses)

**Known Limitations (Acceptable for MVP)**:
- No pulse reporting system (manual moderation via admin dashboard)
- No notifications system (future enhancement)
- No direct messaging (future enhancement)
- No pulse threading/conversations (basic replies only)
- No video support (links only)

---

## Deferred Features

### **Pulse Reporting System** ⏭️

**Why deferred**:
- Product launches as **invitation-only** with curated community
- Manual moderation is sufficient for small, invited user base
- Admin can delete problematic pulses directly via dashboard or database
- Reduces MVP development time and complexity

**When to implement**:
- If community grows beyond ~100-200 active users
- When transitioning to public registration
- If manual moderation becomes unsustainable

**Implementation plan** (when needed):
See [Section 6: Pulse Reporting System](#6-pulse-reporting-system) below for detailed implementation guide.

---

## SEO & Marketing Optimization (Future Session)

> **Status**: Not yet implemented - will be addressed in dedicated session before public launch

### **Critical SEO Requirements**

**Meta Tags (Landing Page)**:
- `<title>` - Unique, keyword-rich page title
- `<meta name="description">` - Compelling 155-character description
- `<meta name="keywords">` - Relevant keywords (less important for modern SEO)
- Open Graph tags for social sharing:
  - `og:title`
  - `og:description`
  - `og:image` - Preview image for social media
  - `og:url`
  - `og:type` - "website"
- Twitter Card tags:
  - `twitter:card` - "summary_large_image"
  - `twitter:title`
  - `twitter:description`
  - `twitter:image`
  - `twitter:creator` - Your Twitter handle

**Technical SEO**:
- Sitemap.xml generation
- Robots.txt configuration
- Canonical URLs
- Structured data (JSON-LD) for organization/website
- Mobile-friendly testing
- Page speed optimization
- Image alt tags on landing page

**Content SEO**:
- Proper heading hierarchy (H1 → H2 → H3)
- Internal linking strategy
- Keyword research and optimization
- URL structure consistency

### **Analytics & Tracking**

**Essential Tools**:
- Google Analytics 4 (GA4) - User behavior tracking
- Google Search Console - Search performance monitoring
- Plausible/Fathom Analytics (privacy-friendly alternative to GA)

**What to Track**:
- Landing page conversion rate (visits → registrations)
- Bounce rate on landing page
- Time on page
- Click-through rate on CTAs
- Search query performance
- Referral sources

### **Performance Optimization**

**Frontend**:
- Code splitting (Vite already handles this)
- Lazy loading images on landing page
- Minify CSS/JS (production build)
- Enable gzip/brotli compression
- CDN for static assets (optional)

**Backend API**:
- Response caching headers
- Database query optimization
- Rate limiting configuration
- CORS configuration for production domain

### **Social Media Preparation**

**Launch Assets**:
- Preview image (1200x630px for social sharing)
- Launch announcement copy
- Screenshots/demos for sharing
- Video walkthrough (optional)

**Channels**:
- LinkedIn post (your primary audience)
- Twitter/X thread
- Reddit (r/programming, r/dotnet, relevant communities)
- Hacker News (if applicable)
- Dev.to or Medium article

### **Domain & Hosting**

**Checklist**:
- [ ] Production domain configured
- [ ] SSL certificate installed (HTTPS)
- [ ] DNS records configured (A, CNAME, MX if applicable)
- [ ] Subdomain for blog (blog.codewrinkles.com)
- [ ] Email forwarding/custom email setup

### **Legal & Compliance**

**Required Pages**:
- Privacy Policy
- Terms of Service
- Cookie Policy (if using cookies beyond essential)
- GDPR compliance (if targeting EU users)
- Contact/Support page

### **Pre-Launch Testing**

**SEO Validation**:
- [ ] Run Google Lighthouse audit (aim for 90+ on all metrics)
- [ ] Test meta tags with Facebook Debugger
- [ ] Test meta tags with Twitter Card Validator
- [ ] Validate structured data with Google Rich Results Test
- [ ] Test mobile responsiveness on real devices
- [ ] Check page load speed (aim for <2s)

**Cross-Browser Testing**:
- [ ] Chrome
- [ ] Firefox
- [ ] Safari
- [ ] Edge
- [ ] Mobile browsers (iOS Safari, Android Chrome)

---

