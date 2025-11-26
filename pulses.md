# Pulse Feature Implementation Plan

> **Purpose**: Comprehensive implementation guide for the Pulse (microblogging) feature. This document serves as a cross-session reference for building the complete Pulse functionality.

**Last Updated**: 2025-11-26

---

## Table of Contents

1. [Domain Concepts & Naming](#domain-concepts--naming)
2. [Technical Decisions](#technical-decisions)
3. [Database Schema](#database-schema)
4. [Project Structure](#project-structure)
5. [Implementation Phases](#implementation-phases)
6. [API Endpoints](#api-endpoints)
7. [DTOs & Contracts](#dtos--contracts)
8. [Code Patterns & References](#code-patterns--references)
9. [Frontend Components](#frontend-components)
10. [Progress Tracking](#progress-tracking)

---

## Domain Concepts & Naming

### Terminology

| Generic Term | Pulse Term | Usage Example |
|--------------|------------|---------------|
| Post | **Pulse** | "Create a new pulse" |
| Repost/Quote Tweet | **Re-pulse** | "Re-pulse with your thoughts" |
| Posted | **Pulsed** | "I pulsed this idea yesterday" |
| Feed | **Pulse Feed** | "Check your pulse feed" |
| Like | **Like** | (keeping standard) |
| Reply | **Reply** | (keeping standard) |

### Domain Entities

- **Pulse**: A single message/post (max 300 characters)
- **Re-pulse**: A pulse that quotes/references another pulse with added commentary
- **PulseImage**: Single image attached to a pulse
- **PulseLinkPreview**: Auto-generated preview for URLs detected in pulse content
- **PulseEngagement**: Denormalized counts (likes, re-pulses, replies, views)
- **PulseLike**: Record of a user liking a pulse

### Pulse Types (Enum)

```csharp
public enum PulseType : byte
{
    Original = 0,    // Standard pulse
    Repulse = 1,     // Quote/re-pulse of another pulse
    Reply = 2        // Reply to another pulse (future)
}
```

---

## Technical Decisions

### Constraints

| Constraint | Value | Rationale |
|------------|-------|-----------|
| Max pulse content length | 300 characters | Concise messaging, similar to Twitter |
| Max images per pulse | 1 (single image) | Simplicity for MVP, no gallery logic |
| Video support | **NOT SUPPORTED** | Complexity of hosting/transcoding; users can link external videos |
| Soft delete | Yes | Preserve data integrity, handle re-pulses of deleted content |

### Business Rules

1. **Re-pulse rules**:
   - Users CAN re-pulse their own pulses
   - Users CAN re-pulse a re-pulse (nested quoting)
   - Re-pulse requires content (cannot be empty quote)

2. **Deleted pulses**:
   - Soft delete (`IsDeleted = true`)
   - Re-pulses of deleted pulses show "This pulse has been deleted" placeholder
   - Deleted pulses don't appear in feeds but data preserved

3. **Feed ordering**:
   - Reverse chronological (newest first)
   - Cursor-based pagination using `CreatedAt` + `Id`

### EF Core Patterns

**CRITICAL: GUID Primary Key Generation**

All entities use EF Core's `ValueGeneratedOnAdd()` for GUID primary keys. This generates **sequential GUIDs** client-side, preventing index fragmentation.

```csharp
// ✅ CORRECT - Let EF Core generate sequential GUID
public static Pulse Create(Guid authorId, string content)
{
    return new Pulse
    {
        // Id is NOT set - EF Core generates it
        AuthorId = authorId,
        Content = content.Trim(),
        // ...
    };
}

// ❌ WRONG - Never do this
Id = Guid.NewGuid()
```

**Reference**: See `IdentityConfiguration.cs` and `ProfileConfiguration.cs` for existing patterns.

---

## Database Schema

### Schema Name: `pulse`

All Pulse-related tables use the `pulse` schema (similar to `identity` schema for auth).

> **⚠️ IMPORTANT: EF Core Migrations Only**
>
> The SQL scripts below are **for documentation/reference purposes only**.
>
> **All schema changes MUST be implemented through:**
> 1. Entity classes in `Codewrinkles.Domain/Pulse/`
> 2. EF Core configurations in `Codewrinkles.Infrastructure/Persistence/Configurations/Pulse/`
> 3. EF Core migrations: `dotnet ef migrations add <MigrationName>`
> 4. Apply migrations: `dotnet ef database update`
>
> **Never execute these SQL scripts directly.** They exist only to visualize the target schema structure.

### Tables (Reference Only)

#### `pulse.Pulses`

```sql
CREATE TABLE pulse.Pulses (
    Id                  UNIQUEIDENTIFIER    PRIMARY KEY,
    AuthorId            UNIQUEIDENTIFIER    NOT NULL,   -- FK → identity.Profiles
    Content             NVARCHAR(300)       NOT NULL,
    RepulsedPulseId     UNIQUEIDENTIFIER    NULL,       -- FK → pulse.Pulses (for re-pulses)
    ParentPulseId       UNIQUEIDENTIFIER    NULL,       -- FK → pulse.Pulses (for replies, future)
    PulseType           TINYINT             NOT NULL    DEFAULT 0,
    CreatedAt           DATETIME2           NOT NULL,
    UpdatedAt           DATETIME2           NULL,
    IsDeleted           BIT                 NOT NULL    DEFAULT 0,

    CONSTRAINT FK_Pulses_Author FOREIGN KEY (AuthorId) REFERENCES identity.Profiles(Id),
    CONSTRAINT FK_Pulses_RepulsedPulse FOREIGN KEY (RepulsedPulseId) REFERENCES pulse.Pulses(Id),
    CONSTRAINT FK_Pulses_ParentPulse FOREIGN KEY (ParentPulseId) REFERENCES pulse.Pulses(Id)
);

-- Indexes
CREATE INDEX IX_Pulses_AuthorId ON pulse.Pulses(AuthorId);
CREATE INDEX IX_Pulses_CreatedAt ON pulse.Pulses(CreatedAt DESC);
CREATE INDEX IX_Pulses_RepulsedPulseId ON pulse.Pulses(RepulsedPulseId) WHERE RepulsedPulseId IS NOT NULL;
```

#### `pulse.PulseImages`

```sql
CREATE TABLE pulse.PulseImages (
    Id          UNIQUEIDENTIFIER    PRIMARY KEY,
    PulseId     UNIQUEIDENTIFIER    NOT NULL,   -- FK → pulse.Pulses
    Url         NVARCHAR(500)       NOT NULL,
    AltText     NVARCHAR(200)       NULL,
    Width       INT                 NULL,
    Height      INT                 NULL,

    CONSTRAINT FK_PulseImages_Pulse FOREIGN KEY (PulseId) REFERENCES pulse.Pulses(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_PulseImages_PulseId ON pulse.PulseImages(PulseId); -- One image per pulse
```

#### `pulse.PulseLinkPreviews`

```sql
CREATE TABLE pulse.PulseLinkPreviews (
    Id          UNIQUEIDENTIFIER    PRIMARY KEY,
    PulseId     UNIQUEIDENTIFIER    NOT NULL,   -- FK → pulse.Pulses
    Url         NVARCHAR(2000)      NOT NULL,
    Title       NVARCHAR(200)       NOT NULL,
    Description NVARCHAR(500)       NULL,
    ImageUrl    NVARCHAR(500)       NULL,
    Domain      NVARCHAR(100)       NOT NULL,

    CONSTRAINT FK_PulseLinkPreviews_Pulse FOREIGN KEY (PulseId) REFERENCES pulse.Pulses(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_PulseLinkPreviews_PulseId ON pulse.PulseLinkPreviews(PulseId); -- One preview per pulse
```

#### `pulse.PulseEngagements`

```sql
CREATE TABLE pulse.PulseEngagements (
    PulseId         UNIQUEIDENTIFIER    PRIMARY KEY,   -- FK → pulse.Pulses
    ReplyCount      INT                 NOT NULL    DEFAULT 0,
    RepulseCount    INT                 NOT NULL    DEFAULT 0,
    LikeCount       INT                 NOT NULL    DEFAULT 0,
    ViewCount       BIGINT              NOT NULL    DEFAULT 0,

    CONSTRAINT FK_PulseEngagements_Pulse FOREIGN KEY (PulseId) REFERENCES pulse.Pulses(Id) ON DELETE CASCADE
);
```

#### `pulse.PulseLikes`

```sql
CREATE TABLE pulse.PulseLikes (
    PulseId     UNIQUEIDENTIFIER    NOT NULL,   -- FK → pulse.Pulses
    ProfileId   UNIQUEIDENTIFIER    NOT NULL,   -- FK → identity.Profiles
    CreatedAt   DATETIME2           NOT NULL,

    CONSTRAINT PK_PulseLikes PRIMARY KEY (PulseId, ProfileId),
    CONSTRAINT FK_PulseLikes_Pulse FOREIGN KEY (PulseId) REFERENCES pulse.Pulses(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PulseLikes_Profile FOREIGN KEY (ProfileId) REFERENCES identity.Profiles(Id)
);

CREATE INDEX IX_PulseLikes_ProfileId ON pulse.PulseLikes(ProfileId);
```

---

## Project Structure

### Backend

```
apps/backend/src/

├── Codewrinkles.API/
│   └── Modules/
│       ├── Identity/                    # (existing)
│       │   └── IdentityEndpoints.cs
│       └── Pulse/                       # NEW
│           └── PulseEndpoints.cs
│
├── Codewrinkles.Application/
│   ├── Common/
│   │   └── Interfaces/
│   │       ├── IUnitOfWork.cs           # Add IPulseRepository
│   │       └── IPulseRepository.cs      # NEW
│   ├── Users/                           # (existing)
│   └── Pulse/                           # NEW
│       ├── CreatePulse.cs
│       ├── CreatePulseValidator.cs
│       ├── GetFeed.cs
│       ├── GetPulse.cs
│       ├── CreateRepulse.cs
│       ├── CreateRepulseValidator.cs
│       ├── LikePulse.cs
│       ├── LikePulseValidator.cs
│       ├── UnlikePulse.cs
│       ├── UnlikePulseValidator.cs
│       └── DeletePulse.cs
│
├── Codewrinkles.Domain/
│   ├── Identity/                        # (existing)
│   └── Pulse/                           # NEW
│       ├── Pulse.cs
│       ├── PulseImage.cs
│       ├── PulseLinkPreview.cs
│       ├── PulseEngagement.cs
│       ├── PulseLike.cs
│       ├── PulseType.cs
│       └── Exceptions/
│           ├── PulseNotFoundException.cs
│           ├── PulseContentTooLongException.cs
│           ├── PulseContentEmptyException.cs
│           └── PulseAlreadyLikedException.cs
│
└── Codewrinkles.Infrastructure/
    └── Persistence/
        ├── Configurations/
        │   ├── Identity/                # (existing)
        │   └── Pulse/                   # NEW
        │       ├── PulseConfiguration.cs
        │       ├── PulseImageConfiguration.cs
        │       ├── PulseLinkPreviewConfiguration.cs
        │       ├── PulseEngagementConfiguration.cs
        │       └── PulseLikeConfiguration.cs
        ├── Repositories/
        │   ├── IdentityRepository.cs    # (existing)
        │   └── PulseRepository.cs       # NEW
        └── ApplicationDbContext.cs      # Add DbSets
```

### Frontend

```
apps/frontend/src/features/pulse/

├── PulsePage.tsx                    # Main 3-column layout (existing)
├── PulseNavigation.tsx              # Left navigation (existing)
├── PulseRightSidebar.tsx            # Right sidebar (existing)
├── WhoToFollow.tsx                  # (existing)
│
├── Composer.tsx                     # Create pulse input (existing, update)
├── PulseCard.tsx                    # Single pulse display (refactor from PostCard)
├── PulseActions.tsx                 # Like, re-pulse, reply, share buttons
├── PulseImage.tsx                   # Image display component
├── PulseLinkPreview.tsx             # Link preview card
├── RepulsedPulse.tsx                # Embedded re-pulsed content
│
├── feed/
│   ├── Feed.tsx                     # Feed list component
│   ├── useFeed.ts                   # API hook for fetching feed
│   └── index.ts
│
├── profile/                         # User's pulse profile
│   ├── ProfilePage.tsx
│   ├── ProfileHeader.tsx
│   ├── ProfilePulses.tsx
│   └── index.ts
│
├── messages/                        # Direct messages (future)
│   └── MessagesPage.tsx
│
├── notifications/                   # Notifications (future)
│   └── NotificationsPage.tsx
│
├── bookmarks/                       # Saved pulses (future)
│   └── BookmarksPage.tsx
│
├── hooks/
│   ├── usePulse.ts                  # Single pulse operations
│   └── usePulseActions.ts           # Like, re-pulse, etc.
│
└── types.ts                         # Pulse-specific TypeScript types
```

---

## Implementation Phases

### Phase 1: Domain & Infrastructure Setup

**Goal**: Establish database schema and core entities

**Tasks**:
1. Create `Codewrinkles.Domain/Pulse/` folder
2. Create `PulseType.cs` enum
3. Create `Pulse.cs` entity with factory method
   - Follow Identity entity pattern
   - Use `#pragma warning disable CS8618` for EF constructor
   - NO `Guid.NewGuid()` - let EF generate
4. Create `PulseEngagement.cs` entity
5. Create domain exceptions in `Pulse/Exceptions/`
6. Create `Codewrinkles.Infrastructure/Persistence/Configurations/Pulse/` folder
7. Create `PulseConfiguration.cs`
   - Schema: `pulse`
   - `ValueGeneratedOnAdd()` for Id
   - Relationships to Profile, self-references
8. Create `PulseEngagementConfiguration.cs`
9. Add `DbSet<Pulse>` and `DbSet<PulseEngagement>` to `ApplicationDbContext`
10. Create `IPulseRepository.cs` interface in Application
11. Create `PulseRepository.cs` in Infrastructure
12. Update `IUnitOfWork` with `IPulseRepository Pulses { get; }`
13. Update `UnitOfWork` implementation
14. Create EF migration: `AddPulseSchema`
15. Apply migration to database

**Validation**: Database has `pulse.Pulses` and `pulse.PulseEngagements` tables

---

### Phase 2: Create Pulse (Text Only)

**Goal**: Users can create text-only pulses

**Backend Tasks**:
1. Create `Codewrinkles.Application/Pulse/` folder
2. Create `CreatePulseCommand` record:
   ```csharp
   public sealed record CreatePulseCommand(
       Guid AuthorId,
       string Content
   ) : ICommand<CreatePulseResult>;
   ```
3. Create `CreatePulseResult` record with PulseId
4. Create `CreatePulseValidator`:
   - Content not empty
   - Content max 300 chars
   - Author (Profile) exists
5. Create `CreatePulseCommandHandler`:
   - Create Pulse entity
   - Create PulseEngagement with zero counts
   - Save and return result
6. Create `Codewrinkles.API/Modules/Pulse/` folder
7. Create `PulseEndpoints.cs` with `POST /api/pulse`
8. Register endpoints in `Program.cs`

**Request/Response**:
```
POST /api/pulse
Authorization: Bearer {token}
Content-Type: application/json

{
  "content": "My first pulse! 🚀"
}

Response 201:
{
  "pulseId": "guid",
  "content": "My first pulse! 🚀",
  "createdAt": "2025-01-01T12:00:00Z"
}
```

**Validation**: Create pulse via API, verify in database

---

### Phase 3: Get Feed

**Goal**: Fetch paginated feed of pulses

**Backend Tasks**:
1. Create `PulseDto` class:
   ```csharp
   public sealed record PulseDto(
       Guid Id,
       PulseAuthorDto Author,
       string Content,
       PulseType Type,
       DateTime CreatedAt,
       PulseEngagementDto Engagement,
       bool IsLikedByCurrentUser,
       RepulsedPulseDto? RepulsedPulse
   );
   ```
2. Create supporting DTOs: `PulseAuthorDto`, `PulseEngagementDto`, `RepulsedPulseDto`
3. Create `GetFeedQuery`:
   ```csharp
   public sealed record GetFeedQuery(
       Guid? CurrentUserId,
       string? Cursor,      // Base64 encoded cursor
       int Limit = 20
   ) : IQuery<GetFeedResult>;
   ```
4. Create `GetFeedQueryHandler`:
   - Query pulses with author, engagement
   - Filter: not deleted
   - Order: CreatedAt DESC, Id DESC
   - Cursor-based pagination
5. Create `GET /api/pulse` endpoint
6. Create `GetPulseQuery` for single pulse: `GET /api/pulse/{id}`

**Cursor Format**:
```
{
  "createdAt": "2025-01-01T12:00:00Z",
  "id": "guid"
}
// Base64 encoded for URL safety
```

**Response**:
```json
{
  "pulses": [...],
  "nextCursor": "base64string",
  "hasMore": true
}
```

**Validation**: Fetch feed, verify pagination works

---

### Phase 4: Wire Frontend to API

**Goal**: Replace mock data with real API calls

**Frontend Tasks**:
1. Update `apps/frontend/src/types.ts`:
   - Rename `Post` → `Pulse`
   - Rename `PostAuthor` → `PulseAuthor`
   - Rename `RepostedPost` → `RepulsedPulse`
   - Remove video-related types
2. Create `features/pulse/types.ts` for Pulse-specific types
3. Create `features/pulse/feed/useFeed.ts` hook:
   - Fetch from `GET /api/pulse`
   - Handle pagination with cursor
   - Loading/error states
4. Update `Feed.tsx` to use `useFeed` hook
5. Update `Composer.tsx` to call `POST /api/pulse`
6. Rename/refactor components:
   - `PostCard.tsx` → `PulseCard.tsx`
   - Update all "post" references to "pulse"
7. Remove all mock data from `PulsePage.tsx`
8. Update `PulseCard` to render real pulse data

**Validation**: Create pulse, see it in feed immediately

---

### Phase 5: Pulse Images

**Goal**: Support single image attachment

**Backend Tasks**:
1. Create `PulseImage.cs` entity
2. Create `PulseImageConfiguration.cs`
   - One-to-one with Pulse (unique index on PulseId)
3. Add `DbSet<PulseImage>` to context
4. Create migration: `AddPulseImages`
5. Create `IPulseImageService` interface:
   ```csharp
   Task<string> SavePulseImageAsync(
       Stream imageStream,
       Guid pulseId,
       CancellationToken ct);
   ```
6. Implement `PulseImageService` (similar to `AvatarService`)
7. Update `CreatePulseCommand` to accept optional image:
   ```csharp
   public sealed record CreatePulseCommand(
       Guid AuthorId,
       string Content,
       Stream? ImageStream
   ) : ICommand<CreatePulseResult>;
   ```
8. Update handler to save image if provided
9. Update `POST /api/pulse` to accept multipart form data
10. Update `PulseDto` to include `ImageUrl?`

**Frontend Tasks**:
1. Update `Composer.tsx` with image upload button
2. Create image preview in composer
3. Handle multipart form submission
4. Update `PulseCard` to display image

**Validation**: Create pulse with image, see it in feed

---

### Phase 6: Re-pulse (Quoted Pulse)

**Goal**: Users can quote/re-pulse other pulses

**Backend Tasks**:
1. Pulse entity already has `RepulsedPulseId` - ensure navigation property
2. Create `CreateRepulseCommand`:
   ```csharp
   public sealed record CreateRepulseCommand(
       Guid AuthorId,
       string Content,
       Guid RepulsedPulseId
   ) : ICommand<CreateRepulseResult>;
   ```
3. Create `CreateRepulseValidator`:
   - Content not empty (re-pulse requires commentary)
   - Content max 300 chars
   - RepulsedPulse exists and not deleted
4. Create `CreateRepulseCommandHandler`:
   - Create pulse with type `Repulse`
   - Increment `RepulseCount` on original pulse
5. Create `POST /api/pulse/repulse` endpoint
6. Update `GetFeedQuery` to eager-load repulsed pulses
7. Update `PulseDto` with `RepulsedPulse` nested object

**Frontend Tasks**:
1. Add re-pulse button to `PulseActions`
2. Create re-pulse modal/dialog
3. Update `PulseCard` to show repulsed content
4. Create `RepulsedPulse.tsx` component for embedded display

**Validation**: Re-pulse a pulse, see nested content in feed

---

### Phase 7: Link Previews

**Goal**: Auto-generate previews for URLs in pulse content

**Backend Tasks**:
1. Create `PulseLinkPreview.cs` entity
2. Create `PulseLinkPreviewConfiguration.cs`
3. Add `DbSet<PulseLinkPreview>` to context
4. Create migration: `AddPulseLinkPreviews`
5. Create `ILinkPreviewService` interface:
   ```csharp
   Task<LinkPreviewData?> FetchPreviewAsync(string url, CancellationToken ct);
   ```
6. Implement `LinkPreviewService`:
   - Fetch URL
   - Parse Open Graph meta tags
   - Extract title, description, image, domain
7. Update `CreatePulseCommandHandler`:
   - Detect URLs in content using regex
   - Fetch preview for first URL (if any)
   - Save `PulseLinkPreview` if successful
8. Update `PulseDto` with `LinkPreview?`

**Frontend Tasks**:
1. Update `PulseCard` to display link preview
2. Create/update `PulseLinkPreview.tsx` component

**URL Detection Regex**:
```csharp
private static readonly Regex UrlRegex = new(
    @"https?://[^\s]+",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);
```

**Validation**: Create pulse with URL, see preview generated

---

### Phase 8: Likes

**Goal**: Users can like/unlike pulses

**Backend Tasks**:
1. Create `PulseLike.cs` entity
2. Create `PulseLikeConfiguration.cs` (composite PK)
3. Add `DbSet<PulseLike>` to context
4. Create migration: `AddPulseLikes`
5. Create `LikePulseCommand`:
   ```csharp
   public sealed record LikePulseCommand(
       Guid ProfileId,
       Guid PulseId
   ) : ICommand<Unit>;
   ```
6. Create `LikePulseValidator`:
   - Pulse exists and not deleted
   - User hasn't already liked
7. Create `LikePulseCommandHandler`:
   - Create PulseLike record
   - Increment `LikeCount` on PulseEngagement
8. Create `UnlikePulseCommand` + Handler (reverse)
9. Create endpoints:
   - `POST /api/pulse/{id}/like`
   - `DELETE /api/pulse/{id}/like`
10. Update `GetFeedQuery` to check if current user liked each pulse

**Frontend Tasks**:
1. Update `PulseActions` with like button
2. Create `usePulseActions` hook for like/unlike
3. Optimistic UI update on like
4. Show filled/unfilled heart based on `isLikedByCurrentUser`

**Validation**: Like pulse, see count increment, unlike, see count decrement

---

### Phase 9: Delete Pulse

**Goal**: Users can delete their own pulses

**Backend Tasks**:
1. Create `DeletePulseCommand`:
   ```csharp
   public sealed record DeletePulseCommand(
       Guid ProfileId,
       Guid PulseId
   ) : ICommand<Unit>;
   ```
2. Create `DeletePulseValidator`:
   - Pulse exists
   - User is the author
3. Create `DeletePulseCommandHandler`:
   - Soft delete: set `IsDeleted = true`
4. Create `DELETE /api/pulse/{id}` endpoint

**Frontend Tasks**:
1. Add delete option to pulse menu (three dots)
2. Confirmation dialog
3. Remove pulse from feed on delete

**Validation**: Delete pulse, verify soft deleted in DB, not shown in feed

---

### Future Phases (Post-MVP)

#### Phase 10: Replies
- Add reply functionality
- Thread view
- Reply counts

#### Phase 11: User Profile Page
- View user's pulses
- Follow/unfollow

#### Phase 12: Notifications
- Like notifications
- Re-pulse notifications
- Reply notifications

#### Phase 13: Bookmarks
- Save pulses
- Bookmarks page

#### Phase 14: Search
- Search pulses by content
- Search users

---

## API Endpoints

### Pulse Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| `POST` | `/api/pulse` | Create pulse | Required |
| `GET` | `/api/pulse` | Get feed (paginated) | Optional* |
| `GET` | `/api/pulse/{id}` | Get single pulse | Optional* |
| `DELETE` | `/api/pulse/{id}` | Delete pulse | Required |
| `POST` | `/api/pulse/repulse` | Create re-pulse | Required |
| `POST` | `/api/pulse/{id}/like` | Like pulse | Required |
| `DELETE` | `/api/pulse/{id}/like` | Unlike pulse | Required |

*Auth optional but needed for `isLikedByCurrentUser`

---

## DTOs & Contracts

### Request DTOs

```csharp
// Create Pulse
public sealed record CreatePulseRequest(string Content);

// Create Re-pulse
public sealed record CreateRepulseRequest(string Content, Guid RepulsedPulseId);
```

### Response DTOs

```csharp
public sealed record PulseDto(
    Guid Id,
    PulseAuthorDto Author,
    string Content,
    string Type,                     // "original", "repulse", "reply"
    DateTime CreatedAt,
    PulseEngagementDto Engagement,
    bool IsLikedByCurrentUser,
    PulseImageDto? Image,
    PulseLinkPreviewDto? LinkPreview,
    RepulsedPulseDto? RepulsedPulse
);

public sealed record PulseAuthorDto(
    Guid Id,
    string Name,
    string Handle,
    string? AvatarUrl
);

public sealed record PulseEngagementDto(
    int ReplyCount,
    int RepulseCount,
    int LikeCount,
    long ViewCount
);

public sealed record PulseImageDto(
    string Url,
    string? AltText
);

public sealed record PulseLinkPreviewDto(
    string Url,
    string Title,
    string? Description,
    string? ImageUrl,
    string Domain
);

public sealed record RepulsedPulseDto(
    Guid Id,
    PulseAuthorDto Author,
    string Content,
    DateTime CreatedAt,
    bool IsDeleted,
    PulseImageDto? Image,
    PulseLinkPreviewDto? LinkPreview
);

public sealed record FeedResponse(
    IReadOnlyList<PulseDto> Pulses,
    string? NextCursor,
    bool HasMore
);
```

---

## Code Patterns & References

### Entity Pattern (Follow Identity.cs)

```csharp
public sealed class Pulse
{
    public Guid Id { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; }
    public Guid? RepulsedPulseId { get; private set; }
    public PulseType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Navigation properties
    public Profile Author { get; private set; }
    public Pulse? RepulsedPulse { get; private set; }
    public PulseEngagement Engagement { get; private set; }
    public PulseImage? Image { get; private set; }
    public PulseLinkPreview? LinkPreview { get; private set; }

#pragma warning disable CS8618
    private Pulse() { } // EF Core
#pragma warning restore CS8618

    public static Pulse Create(Guid authorId, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (content.Length > 300)
            throw new PulseContentTooLongException(content.Length);

        return new Pulse
        {
            // Id generated by EF Core (sequential GUID)
            AuthorId = authorId,
            Content = content.Trim(),
            Type = PulseType.Original,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public static Pulse CreateRepulse(Guid authorId, string content, Guid repulsedPulseId)
    {
        var pulse = Create(authorId, content);
        pulse.Type = PulseType.Repulse;
        pulse.RepulsedPulseId = repulsedPulseId;
        return pulse;
    }

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### EF Configuration Pattern (Follow IdentityConfiguration.cs)

```csharp
public sealed class PulseConfiguration : IEntityTypeConfiguration<Pulse>
{
    public void Configure(EntityTypeBuilder<Pulse> builder)
    {
        builder.ToTable("Pulses", "pulse");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.Content)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasConversion<byte>()
            .IsRequired();

        builder.HasOne(p => p.Author)
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.RepulsedPulse)
            .WithMany()
            .HasForeignKey(p => p.RepulsedPulseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Engagement)
            .WithOne()
            .HasForeignKey<PulseEngagement>(e => e.PulseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.AuthorId);
        builder.HasIndex(p => p.CreatedAt).IsDescending();
    }
}
```

### Validator Pattern (Follow LoginUserValidator.cs)

```csharp
public sealed class CreatePulseValidator : IValidator<CreatePulseCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreatePulseValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreatePulseCommand command,
        CancellationToken cancellationToken)
    {
        var result = new ValidationResult();

        // Input validation
        if (string.IsNullOrWhiteSpace(command.Content))
            result.AddError("Content", "Pulse content is required.");
        else if (command.Content.Length > 300)
            result.AddError("Content", "Pulse content cannot exceed 300 characters.");

        if (result.HasErrors)
            return result;

        // Business rule validation (async)
        await ValidateAuthorExistsAsync(command.AuthorId, cancellationToken);

        return result;
    }

    private async Task ValidateAuthorExistsAsync(Guid authorId, CancellationToken ct)
    {
        var profile = await _unitOfWork.Profiles.FindByIdAsync(authorId, ct);
        if (profile is null)
            throw new ProfileNotFoundException(authorId);
    }
}
```

---

## Frontend Components

### TypeScript Types (apps/frontend/src/features/pulse/types.ts)

```typescript
export interface Pulse {
  id: string;
  author: PulseAuthor;
  content: string;
  type: 'original' | 'repulse' | 'reply';
  createdAt: string;
  engagement: PulseEngagement;
  isLikedByCurrentUser: boolean;
  image?: PulseImage;
  linkPreview?: PulseLinkPreview;
  repulsedPulse?: RepulsedPulse;
}

export interface PulseAuthor {
  id: string;
  name: string;
  handle: string;
  avatarUrl?: string;
}

export interface PulseEngagement {
  replyCount: number;
  repulseCount: number;
  likeCount: number;
  viewCount: number;
}

export interface PulseImage {
  url: string;
  altText?: string;
}

export interface PulseLinkPreview {
  url: string;
  title: string;
  description?: string;
  imageUrl?: string;
  domain: string;
}

export interface RepulsedPulse {
  id: string;
  author: PulseAuthor;
  content: string;
  createdAt: string;
  isDeleted: boolean;
  image?: PulseImage;
  linkPreview?: PulseLinkPreview;
}

export interface FeedResponse {
  pulses: Pulse[];
  nextCursor: string | null;
  hasMore: boolean;
}
```

### API Hook Pattern

```typescript
// features/pulse/feed/useFeed.ts
import { useState, useEffect, useCallback } from 'react';
import type { Pulse, FeedResponse } from '../types';
import { config } from '../../../config';

export function useFeed() {
  const [pulses, setPulses] = useState<Pulse[]>([]);
  const [cursor, setCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchFeed = useCallback(async (nextCursor?: string) => {
    setIsLoading(true);
    setError(null);

    try {
      const url = new URL(`${config.api.baseUrl}/api/pulse`);
      if (nextCursor) url.searchParams.set('cursor', nextCursor);

      const response = await fetch(url.toString(), {
        headers: {
          'Authorization': `Bearer ${getAccessToken()}`,
        },
      });

      if (!response.ok) throw new Error('Failed to fetch feed');

      const data: FeedResponse = await response.json();

      setPulses(prev => nextCursor ? [...prev, ...data.pulses] : data.pulses);
      setCursor(data.nextCursor);
      setHasMore(data.hasMore);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const loadMore = useCallback(() => {
    if (cursor && hasMore && !isLoading) {
      fetchFeed(cursor);
    }
  }, [cursor, hasMore, isLoading, fetchFeed]);

  useEffect(() => {
    fetchFeed();
  }, [fetchFeed]);

  return { pulses, isLoading, error, hasMore, loadMore, refetch: fetchFeed };
}
```

---

## Progress Tracking

### Completed

- [x] Plan created (this document)

### In Progress

- [ ] Phase 1: Domain & Infrastructure Setup

### Pending

- [ ] Phase 2: Create Pulse (Text Only)
- [ ] Phase 3: Get Feed
- [ ] Phase 4: Wire Frontend to API
- [ ] Phase 5: Pulse Images
- [ ] Phase 6: Re-pulse
- [ ] Phase 7: Link Previews
- [ ] Phase 8: Likes
- [ ] Phase 9: Delete Pulse

### Blocked / Future

- [ ] Phase 10: Replies
- [ ] Phase 11: User Profile Page
- [ ] Phase 12: Notifications
- [ ] Phase 13: Bookmarks
- [ ] Phase 14: Search

---

## Session Notes

### 2025-11-26 - Initial Planning Session

**Decisions Made**:
- Domain naming: "Pulse" not "Post"
- Max content: 300 characters
- Single image only (no galleries)
- No native video support
- Soft delete for pulses
- Sequential GUID generation via EF Core
- Cursor-based pagination for feed
- Re-pulse = quoted pulse with required commentary

**Questions Resolved**:
- Can re-pulse own pulse: Yes
- Can re-pulse a re-pulse: Yes
- Re-pulse of deleted pulse: Shows placeholder

**Next Session**: Start Phase 1 - Domain & Infrastructure Setup

---

*Add notes here for each implementation session*
