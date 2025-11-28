# Follow System Implementation Plan

> **Purpose**: Complete technical specification for implementing the Follow system in Pulse. This transforms the current "public firehose" feed into a personalized social network.

**Last Updated**: 2025-11-28

---

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Domain Model Design](#domain-model-design)
3. [Database Schema via EF Core](#database-schema-via-ef-core)
4. [Follow Suggestions Strategy](#follow-suggestions-strategy)
5. [Feed Filtering Strategy](#feed-filtering-strategy)
6. [Folder Structure](#folder-structure)
7. [Step-by-Step Implementation Plan](#step-by-step-implementation-plan)
8. [Performance & Scaling](#performance--scaling)
9. [Testing Strategy](#testing-strategy)
10. [Success Criteria](#success-criteria)

---

## Executive Summary

The Follow system is the **most critical missing feature** for Pulse. It transforms the current "public firehose" feed into a personalized social network, aligning with core.md's vision: *"content should be discovered by its value, not by virality"*.

### Why This Matters
- ✅ Transforms feed from showing ALL pulses to showing only followed users
- ✅ Enables user profiles with follower/following counts
- ✅ Unblocks notifications (follow notifications)
- ✅ Enables "People You May Know" suggestions via 2-hop graph traversal
- ✅ Makes Pulse actually useful as a social network

### Architecture Approach
- **Domain**: New `/Social/` domain (peer to Identity and Pulse)
- **Database**: EF Core configuration with composite primary key + bidirectional indexes
- **Queries**: Standard LINQ (no graph DB needed)
- **Scale**: Designed for < 10K users, < 1K follows per user initially
- **Pattern**: Follows all established codebase conventions

---

## Domain Model Design

### Decision: Create NEW `/Social/` Domain

**Rationale:**
- Follows are about **people relationships**, not posts (Pulse) or auth (Identity)
- Clean separation of concerns
- Future-proof for: Mute, Block, Lists, Friend Requests
- Matches codebase pattern (Identity, Pulse, Social as peer domains)

### Follow Entity

**File**: `apps/backend/src/Codewrinkles.Domain/Social/Follow.cs`

```csharp
namespace Codewrinkles.Domain.Social;

/// <summary>
/// Represents a follow relationship between two profiles.
/// Composite primary key: (FollowerId, FollowingId)
/// </summary>
public sealed class Follow
{
    // Private parameterless constructor for EF Core materialization only
    // EF Core will populate all properties via reflection when loading from database
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private Follow() { }
#pragma warning restore CS8618

    // Properties - Composite Primary Key
    public Guid FollowerId { get; private set; }   // Who is following
    public Guid FollowingId { get; private set; }  // Who is being followed
    public DateTime CreatedAt { get; private set; }

    // Navigation properties (cross-domain reference to Identity.Profile)
    public Profile Follower { get; private set; }
    public Profile Following { get; private set; }

    // Factory method
    public static Follow Create(Guid followerId, Guid followingId)
    {
        ArgumentException.ThrowIfNullOrEmpty(followerId.ToString(), nameof(followerId));
        ArgumentException.ThrowIfNullOrEmpty(followingId.ToString(), nameof(followingId));

        if (followerId == followingId)
        {
            throw new FollowSelfException("Cannot follow yourself");
        }

        return new Follow
        {
            FollowerId = followerId,
            FollowingId = followingId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

**Key Design Decisions:**
- **Composite Primary Key**: `(FollowerId, FollowingId)` prevents duplicate follows
- **No soft delete**: Hard delete on unfollow (no audit trail needed for MVP)
- **Immutable**: No update methods (follows are created or deleted, never modified)
- **Cross-domain navigation**: References `Profile` from Identity domain

### Exception Classes

**File**: `apps/backend/src/Codewrinkles.Domain/Social/Exceptions/FollowSelfException.cs`

```csharp
namespace Codewrinkles.Domain.Social.Exceptions;

public sealed class FollowSelfException : Exception
{
    public FollowSelfException(string message) : base(message) { }
}
```

**File**: `apps/backend/src/Codewrinkles.Domain/Social/Exceptions/AlreadyFollowingException.cs`

```csharp
namespace Codewrinkles.Domain.Social.Exceptions;

public sealed class AlreadyFollowingException : Exception
{
    public AlreadyFollowingException(Guid followerId, Guid followingId)
        : base($"Profile {followerId} is already following {followingId}") { }
}
```

**File**: `apps/backend/src/Codewrinkles.Domain/Social/Exceptions/NotFollowingException.cs`

```csharp
namespace Codewrinkles.Domain.Social.Exceptions;

public sealed class NotFollowingException : Exception
{
    public NotFollowingException(Guid followerId, Guid followingId)
        : base($"Profile {followerId} is not following {followingId}") { }
}
```

---

## Database Schema via EF Core

### EF Core Entity Configuration

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Social/FollowConfiguration.cs`

```csharp
using Codewrinkles.Domain.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Social;

public sealed class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        // Table mapping - will create [social].[Follows] table
        builder.ToTable("Follows", "social");

        // Composite Primary Key
        // This creates a clustered index on (FollowerId, FollowingId)
        builder.HasKey(f => new { f.FollowerId, f.FollowingId });

        // Properties
        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // Relationships (cross-domain to Identity.Profile)
        builder.HasOne(f => f.Follower)
            .WithMany() // Profile doesn't need reverse navigation (one-way)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade deletes

        builder.HasOne(f => f.Following)
            .WithMany()
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);

        // CRITICAL INDEXES FOR PERFORMANCE

        // Index 1: Get all people I'm following (for feed query)
        // Covering index includes CreatedAt to avoid key lookups
        builder.HasIndex(f => f.FollowerId)
            .HasDatabaseName("IX_Follows_FollowerId_FollowingId")
            .IncludeProperties(f => new { f.FollowingId, f.CreatedAt });

        // Index 2: Get all my followers (for follower list + suggestions)
        // Covering index includes CreatedAt to avoid key lookups
        builder.HasIndex(f => f.FollowingId)
            .HasDatabaseName("IX_Follows_FollowingId_FollowerId")
            .IncludeProperties(f => new { f.FollowerId, f.CreatedAt });

        // Note: No need for index on (FollowerId, FollowingId)
        // because composite PK already creates this index
    }
}
```

**Index Strategy:**
1. **Composite PK**: Automatic clustered index on `(FollowerId, FollowingId)` prevents duplicates + fast lookups
2. **Bidirectional indexes**: Covering indexes for both directions (follower→following, following→follower)
3. **INCLUDE clause**: Includes `CreatedAt` to avoid key lookups (index-only scans)

### Update ApplicationDbContext

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/ApplicationDbContext.cs`

```csharp
// Add to existing DbSets
public DbSet<Follow> Follows => Set<Follow>();
```

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/ApplicationDbContext.cs` (OnModelCreating)

```csharp
// Add to existing configuration
modelBuilder.ApplyConfiguration(new FollowConfiguration());
```

### EF Core Migration

**Generate Migration:**
```bash
cd apps/backend/src/Codewrinkles.API
dotnet ef migrations add AddSocialFollowSystem
```

**Apply Migration:**
```bash
dotnet ef database update
```

**What EF Core Will Generate:**
- `[social]` schema
- `Follows` table with columns: FollowerId, FollowingId, CreatedAt
- Composite PRIMARY KEY constraint on (FollowerId, FollowingId)
- Two foreign key constraints to [identity].[Profiles]
- Two non-clustered covering indexes
- CHECK constraint preventing self-follows (if supported by provider)

### Denormalization Decision

**Decision: Do NOT denormalize follower/following counts initially**

**Rationale:**
- Simple COUNT queries are fast until 100K+ followers
- Avoids consistency issues (denormalized counts can drift)
- Easier to implement (no triggers or event handlers needed)
- Profile UPDATE frequency would increase (lock contention)

**When to add denormalization:**
- When specific profiles exceed 10K+ followers
- When profile page loads become slow (> 200ms for count queries)
- Solution: Add `FollowerCount`, `FollowingCount` to Profile + background sync job

---

## Follow Suggestions Strategy

### Algorithm: "People You May Know"

**Query Logic:**
```
Find profiles who:
1. Are followed by people I follow (2-hop relationship)
2. I am NOT already following
3. Are NOT me
4. Ranked by: Number of mutual follows (social proof)
```

### LINQ Implementation

**Repository Method:**

```csharp
public async Task<IReadOnlyList<ProfileSuggestion>> GetSuggestedProfilesAsync(
    Guid currentUserId,
    int limit,
    CancellationToken cancellationToken)
{
    // Step 1: Get all people I'm following
    var myFollowingIds = await _context.Follows
        .Where(f => f.FollowerId == currentUserId)
        .Select(f => f.FollowingId)
        .ToListAsync(cancellationToken);

    if (myFollowingIds.Count == 0)
    {
        // Not following anyone - can't suggest based on follows
        // Could return popular users or recent users instead
        return new List<ProfileSuggestion>();
    }

    // Step 2: Get people followed by people I follow (2-hop)
    // Group by suggested profile and count mutual follows
    var suggestions = await _context.Follows
        .Where(f => myFollowingIds.Contains(f.FollowerId)) // My follows' follows
        .Where(f => f.FollowingId != currentUserId)        // Exclude myself
        .Where(f => !myFollowingIds.Contains(f.FollowingId)) // Exclude people I already follow
        .GroupBy(f => f.FollowingId)
        .Select(g => new
        {
            ProfileId = g.Key,
            MutualFollowCount = g.Count() // How many of my follows also follow them
        })
        .OrderByDescending(x => x.MutualFollowCount)
        .Take(limit)
        .ToListAsync(cancellationToken);

    if (suggestions.Count == 0)
    {
        return new List<ProfileSuggestion>();
    }

    // Step 3: Join with profiles to get user info
    var profileIds = suggestions.Select(s => s.ProfileId).ToList();
    var profiles = await _context.Profiles
        .Where(p => profileIds.Contains(p.Id))
        .ToListAsync(cancellationToken);

    // Step 4: Combine and return
    var result = suggestions
        .Join(profiles,
            s => s.ProfileId,
            p => p.Id,
            (s, p) => new ProfileSuggestion(
                ProfileId: p.Id,
                Name: p.Name,
                Handle: p.Handle ?? string.Empty,
                AvatarUrl: p.AvatarUrl,
                Bio: p.Bio,
                MutualFollowCount: s.MutualFollowCount))
        .ToList();

    return result;
}
```

**Performance:**
- Uses existing indexes (`IX_Follows_FollowerId`, `IX_Follows_FollowingId`)
- Three-query approach (clearer than complex JOIN)
- `Contains()` translates to SQL `IN` (efficient for < 1000 IDs)
- `GroupBy` + `Count()` efficiently calculates mutual follows

**DTO:**

```csharp
public sealed record ProfileSuggestion(
    Guid ProfileId,
    string Name,
    string Handle,
    string? AvatarUrl,
    string? Bio,
    int MutualFollowCount
);
```

---

## Feed Filtering Strategy

### Current Feed Query (Shows ALL Pulses)

```csharp
// PulseRepository.GetFeedAsync() - CURRENT IMPLEMENTATION
var query = _pulses
    .AsNoTracking()
    .Where(p => !p.IsDeleted)  // Only filter: not deleted
    .OrderByDescending(p => p.CreatedAt)
    .ThenByDescending(p => p.Id);
```

**Problem**: This shows every pulse from every user (public firehose).

### NEW Feed Query (Followed Users Only)

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/PulseRepository.cs`

**Update Signature:**
```csharp
public async Task<IReadOnlyList<Pulse>> GetFeedAsync(
    Guid? currentUserId,  // NEW: Optional for authenticated users
    int limit,
    DateTime? beforeCreatedAt,
    Guid? beforeId,
    CancellationToken cancellationToken)
```

**Updated Implementation:**

```csharp
public async Task<IReadOnlyList<Pulse>> GetFeedAsync(
    Guid? currentUserId,
    int limit,
    DateTime? beforeCreatedAt,
    Guid? beforeId,
    CancellationToken cancellationToken)
{
    IQueryable<Pulse> query;

    if (currentUserId.HasValue)
    {
        // AUTHENTICATED USER: Show pulses from followed users + own pulses

        // Step 1: Get IDs of users I'm following
        var followingIds = await _context.Follows
            .Where(f => f.FollowerId == currentUserId.Value)
            .Select(f => f.FollowingId)
            .ToListAsync(cancellationToken);

        // Step 2: Include my own ID (see my own pulses)
        followingIds.Add(currentUserId.Value);

        if (followingIds.Count == 1) // Only contains currentUserId
        {
            // Not following anyone - return empty feed
            // UI will show "Follow people to see their pulses"
            return new List<Pulse>();
        }

        // Step 3: Get pulses from followed users + myself
        query = _pulses
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Engagement)
            .Include(p => p.Image)
            .Include(p => p.RepulsedPulse)
                .ThenInclude(rp => rp!.Author)
            .Where(p => !p.IsDeleted && followingIds.Contains(p.AuthorId));
    }
    else
    {
        // UNAUTHENTICATED USER: Show global feed (or nothing)
        // Decision: Return empty for unauthenticated users
        return new List<Pulse>();
    }

    // Cursor pagination
    if (beforeCreatedAt.HasValue && beforeId.HasValue)
    {
        query = query.Where(p =>
            p.CreatedAt < beforeCreatedAt.Value ||
            (p.CreatedAt == beforeCreatedAt.Value && p.Id.CompareTo(beforeId.Value) < 0));
    }

    // Order by CreatedAt DESC (newest first)
    query = query
        .OrderByDescending(p => p.CreatedAt)
        .ThenByDescending(p => p.Id)
        .Take(limit);

    return await query.ToListAsync<Pulse>(cancellationToken);
}
```

**Performance Considerations:**
- Two-query approach: First get following IDs, then filter pulses
- Uses `IX_Follows_FollowerId` index for fast lookup
- `Contains()` translates to SQL `IN` (efficient for < 1000 IDs)
- At scale: If following > 5000 users, consider caching following IDs

**Update GetFeedQuery Handler:**

```csharp
// Application/Pulse/GetFeed.cs
public sealed record GetFeedQuery(
    Guid? CurrentUserId,  // NOW REQUIRED for filtered feed
    string? Cursor,
    int Limit = 20
) : ICommand<FeedResponse>;
```

---

## Folder Structure

### Backend Files to Create

```
apps/backend/src/
├── Codewrinkles.Domain/
│   └── Social/                          # NEW DOMAIN
│       ├── Follow.cs
│       └── Exceptions/
│           ├── FollowSelfException.cs
│           ├── AlreadyFollowingException.cs
│           └── NotFollowingException.cs
│
├── Codewrinkles.Application/
│   ├── Common/Interfaces/
│   │   └── IFollowRepository.cs        # NEW
│   └── Social/                          # NEW FEATURE FOLDER
│       ├── FollowUser.cs               # Command + Handler
│       ├── FollowUserValidator.cs
│       ├── UnfollowUser.cs
│       ├── UnfollowUserValidator.cs
│       ├── GetFollowers.cs             # Query
│       ├── GetFollowing.cs
│       ├── GetSuggestedProfiles.cs
│       ├── IsFollowing.cs
│       └── SocialDtos.cs
│
├── Codewrinkles.Infrastructure/
│   └── Persistence/
│       ├── ApplicationDbContext.cs      # Update: Add DbSet<Follow>
│       ├── UnitOfWork.cs                # Update: Add IFollowRepository
│       ├── Configurations/Social/
│       │   └── FollowConfiguration.cs
│       ├── Repositories/
│       │   └── FollowRepository.cs
│       └── Migrations/
│           └── {timestamp}_AddSocialFollowSystem.cs
│
└── Codewrinkles.API/
    ├── Modules/Social/
    │   └── SocialEndpoints.cs
    └── Program.cs                       # Update: app.MapSocialEndpoints()
```

### Frontend Files to Create

```
apps/frontend/src/
├── features/
│   └── social/                         # NEW FEATURE FOLDER
│       ├── components/
│       │   ├── FollowButton.tsx        # Follow/Unfollow button
│       │   ├── FollowersList.tsx       # List of followers
│       │   ├── FollowingList.tsx       # List of following
│       │   └── SuggestedProfileCard.tsx
│       ├── hooks/
│       │   ├── useFollow.ts            # Follow mutation
│       │   ├── useUnfollow.ts
│       │   ├── useFollowers.ts         # Get followers list
│       │   ├── useFollowing.ts
│       │   ├── useIsFollowing.ts       # Check if following
│       │   └── useSuggestedProfiles.ts
│       └── index.ts                    # Barrel export
│
├── services/
│   └── socialApi.ts                    # NEW: Social API methods
│
└── types.ts                            # Update: Add Social-related types
```

---

## Step-by-Step Implementation Plan

### Phase 1: Backend Foundation (Domain + Database) ✅ COMPLETED

#### Step 1.1: Create Domain Entity ✅
- [x] Create folder `Domain/Social/`
- [x] Create `Follow.cs` entity with factory method
- [x] Create `Exceptions/` subfolder
- [x] Create exception classes:
  - `FollowSelfException.cs`
  - `AlreadyFollowingException.cs`
  - `NotFollowingException.cs`
- [ ] Add XML documentation to all public members

#### Step 1.2: Create Repository Interface ✅
- [x] Create `IFollowRepository.cs` in `Application/Common/Interfaces/`
- [x] Define methods:

```csharp
public interface IFollowRepository
{
    // Queries
    Task<bool> IsFollowingAsync(Guid followerId, Guid followingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Profile>> GetFollowersAsync(Guid profileId, int limit, DateTime? beforeCreatedAt, Guid? beforeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Profile>> GetFollowingAsync(Guid profileId, int limit, DateTime? beforeCreatedAt, Guid? beforeId, CancellationToken cancellationToken);
    Task<int> GetFollowerCountAsync(Guid profileId, CancellationToken cancellationToken);
    Task<int> GetFollowingCountAsync(Guid profileId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProfileSuggestion>> GetSuggestedProfilesAsync(Guid currentUserId, int limit, CancellationToken cancellationToken);
    Task<Follow?> FindFollowAsync(Guid followerId, Guid followingId, CancellationToken cancellationToken);

    // Commands
    void CreateFollow(Follow follow);
    void DeleteFollow(Follow follow);
}
```

#### Step 1.3: EF Core Configuration ✅
- [x] Create folder `Infrastructure/Persistence/Configurations/Social/`
- [x] Create `FollowConfiguration.cs` (see schema section above)
- [x] Add `DbSet<Follow> Follows` to `ApplicationDbContext`
- [x] Apply configuration in `OnModelCreating`:
  ```csharp
  modelBuilder.ApplyConfiguration(new FollowConfiguration());
  ```
- [x] Update `UnitOfWork.cs`:
  - Add property: `IFollowRepository Follows { get; }`
  - Initialize in constructor

#### Step 1.4: Create Migration ✅
- [x] Run command: `dotnet ef migrations add AddSocialFollowSystem`
- [x] Review generated migration file
- [x] Verify schema, table, indexes, foreign keys are correct
- [x] Apply migration: `dotnet ef database update`
- [x] Verify in database:
  - `[social]` schema exists
  - `Follows` table exists with correct columns
  - Indexes are created

#### Step 1.5: Implement Repository ✅
- [x] Create `Infrastructure/Persistence/Repositories/FollowRepository.cs`
- [x] Implement all interface methods
- [x] Use `_context.Follows` DbSet
- [x] Follow patterns from `PulseRepository` (AsNoTracking for queries, Include for eager loading)
- [x] Register in DI container:
  ```csharp
  // Infrastructure/DependencyInjection.cs
  services.AddScoped<IFollowRepository, FollowRepository>();
  ```

---

### Phase 2: Backend Commands & Queries ✅ COMPLETED

#### Step 2.1: FollowUser Command ✅
- [x] Create `Application/Social/FollowUser.cs`
- [x] Define command record:
  ```csharp
  public sealed record FollowUserCommand(
      Guid FollowerId,
      Guid FollowingId
  ) : ICommand<FollowResult>;

  public sealed record FollowResult(bool Success);
  ```
- [x] Implement `FollowUserCommandHandler`:
  - Use UnitOfWork
  - Create Follow entity via factory method
  - Save to repository
  - Return result

- [x] Create `FollowUserValidator.cs`:
  - Validate both profiles exist (use `IProfileRepository`)
  - Prevent self-follow
  - Prevent duplicate follow (use `IFollowRepository.FindFollowAsync()`)
  - Throw `FollowSelfException` if attempting to follow self
  - Throw `AlreadyFollowingException` if already following

#### Step 2.2: UnfollowUser Command ✅
- [x] Create `Application/Social/UnfollowUser.cs`
- [x] Define command record:
  ```csharp
  public sealed record UnfollowUserCommand(
      Guid FollowerId,
      Guid FollowingId
  ) : ICommand<UnfollowResult>;

  public sealed record UnfollowResult(bool Success);
  ```
- [x] Implement `UnfollowUserCommandHandler`:
  - Fetch Follow entity
  - Delete via repository
  - Return result

- [x] Create `UnfollowUserValidator.cs`:
  - Validate profiles exist
  - Validate follow exists
  - Throw `NotFollowingException` if not following

#### Step 2.3: Query Handlers ✅
- [x] Create `GetFollowers.cs`:
  - Query: Returns paginated list of followers
  - Use cursor-based pagination (beforeCreatedAt, beforeId)
  - Return DTO with Profile info

- [x] Create `GetFollowing.cs`:
  - Query: Returns paginated list of following
  - Use cursor pagination
  - Return DTO with Profile info

- [x] Create `IsFollowing.cs`:
  - Query: Returns boolean
  - Simple lookup via `IFollowRepository.IsFollowingAsync()`

- [x] Create `GetSuggestedProfiles.cs`:
  - Query: Implements 2-hop algorithm (see Follow Suggestions section)
  - Returns `ProfileSuggestion` DTOs with mutual follow count

#### Step 2.4: DTOs ✅
- [x] Create `Application/Social/SocialDtos.cs`:

```csharp
public sealed record FollowResult(bool Success);

public sealed record UnfollowResult(bool Success);

public sealed record FollowerDto(
    Guid ProfileId,
    string Name,
    string Handle,
    string? AvatarUrl,
    string? Bio,
    DateTime FollowedAt
);

public sealed record FollowingDto(
    Guid ProfileId,
    string Name,
    string Handle,
    string? AvatarUrl,
    string? Bio,
    DateTime FollowedAt
);

public sealed record ProfileSuggestion(
    Guid ProfileId,
    string Name,
    string Handle,
    string? AvatarUrl,
    string? Bio,
    int MutualFollowCount
);

public sealed record FollowersResponse(
    IReadOnlyList<FollowerDto> Followers,
    int TotalCount,
    string? NextCursor,
    bool HasMore
);

public sealed record FollowingResponse(
    IReadOnlyList<FollowingDto> Following,
    int TotalCount,
    string? NextCursor,
    bool HasMore
);

public sealed record SuggestedProfilesResponse(
    IReadOnlyList<ProfileSuggestion> Suggestions
);
```

---

### Phase 3: Backend API Endpoints ✅ COMPLETED

#### Step 3.1: Create Endpoints ✅
- [x] Create `API/Modules/Social/SocialEndpoints.cs`
- [x] Implement static class with MapSocialEndpoints method
- [x] Map routes:

```csharp
public static class SocialEndpoints
{
    public static void MapSocialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/social")
            .WithTags("Social");

        // Follow a user
        group.MapPost("{profileId:guid}/follow", FollowUser)
            .WithName("FollowUser")
            .RequireAuthorization();

        // Unfollow a user
        group.MapDelete("{profileId:guid}/follow", UnfollowUser)
            .WithName("UnfollowUser")
            .RequireAuthorization();

        // Get followers of a profile
        group.MapGet("{profileId:guid}/followers", GetFollowers)
            .WithName("GetFollowers");

        // Get who a profile is following
        group.MapGet("{profileId:guid}/following", GetFollowing)
            .WithName("GetFollowing");

        // Check if current user is following a profile
        group.MapGet("{profileId:guid}/is-following", IsFollowing)
            .WithName("IsFollowing")
            .RequireAuthorization();

        // Get suggested profiles to follow
        group.MapGet("suggestions", GetSuggestedProfiles)
            .WithName("GetSuggestedProfiles")
            .RequireAuthorization();
    }

    private static async Task<IResult> FollowUser(
        HttpContext httpContext,
        Guid profileId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var currentUserId = httpContext.GetCurrentProfileId();
        var command = new FollowUserCommand(currentUserId, profileId);
        var result = await mediator.SendAsync(command, cancellationToken);
        return Results.Ok(result);
    }

    // ... implement other endpoint handlers
}
```

- [x] Map in `Program.cs`:
  ```csharp
  app.MapSocialEndpoints();
  ```

#### Step 3.2: Update Feed Endpoint ✅
- [x] Modify `GetFeedQuery` to include `CurrentUserId`
- [x] Update `PulseRepository.GetFeedAsync()` implementation (see Feed Filtering section)
- [x] Update `PulseEndpoints.GetFeed()` to extract userId from JWT
- [ ] Handle empty feed case in frontend (will be done in Phase 5)

---

### Phase 4: Frontend Infrastructure

#### Step 4.1: API Service
- [ ] Create `services/socialApi.ts`:

```typescript
import type {
  FollowResult,
  FollowersResponse,
  FollowingResponse,
  SuggestedProfilesResponse
} from "../types";
import { config } from "../config";
import { apiRequest } from "../utils/api";

export const socialApi = {
  followUser(profileId: string): Promise<FollowResult> {
    return apiRequest<FollowResult>(
      `${config.api.baseUrl}/api/social/${profileId}/follow`,
      { method: "POST" }
    );
  },

  unfollowUser(profileId: string): Promise<FollowResult> {
    return apiRequest<FollowResult>(
      `${config.api.baseUrl}/api/social/${profileId}/follow`,
      { method: "DELETE" }
    );
  },

  getFollowers(profileId: string, params?: { cursor?: string; limit?: number }): Promise<FollowersResponse> {
    const url = new URL(`${config.api.baseUrl}/api/social/${profileId}/followers`);
    if (params?.cursor) url.searchParams.set("cursor", params.cursor);
    if (params?.limit) url.searchParams.set("limit", params.limit.toString());
    return apiRequest<FollowersResponse>(url.toString(), { method: "GET" });
  },

  getFollowing(profileId: string, params?: { cursor?: string; limit?: number }): Promise<FollowingResponse> {
    const url = new URL(`${config.api.baseUrl}/api/social/${profileId}/following`);
    if (params?.cursor) url.searchParams.set("cursor", params.cursor);
    if (params?.limit) url.searchParams.set("limit", params.limit.toString());
    return apiRequest<FollowingResponse>(url.toString(), { method: "GET" });
  },

  isFollowing(profileId: string): Promise<{ isFollowing: boolean }> {
    return apiRequest<{ isFollowing: boolean }>(
      `${config.api.baseUrl}/api/social/${profileId}/is-following`,
      { method: "GET" }
    );
  },

  getSuggestedProfiles(limit?: number): Promise<SuggestedProfilesResponse> {
    const url = new URL(`${config.api.baseUrl}/api/social/suggestions`);
    if (limit) url.searchParams.set("limit", limit.toString());
    return apiRequest<SuggestedProfilesResponse>(url.toString(), { method: "GET" });
  }
};
```

#### Step 4.2: TypeScript Types
- [ ] Add to `types.ts`:

```typescript
// Social/Follow Types
export interface FollowResult {
  success: boolean;
}

export interface FollowerDto {
  profileId: string;
  name: string;
  handle: string;
  avatarUrl: string | null;
  bio: string | null;
  followedAt: string;
}

export interface FollowingDto {
  profileId: string;
  name: string;
  handle: string;
  avatarUrl: string | null;
  bio: string | null;
  followedAt: string;
}

export interface ProfileSuggestion {
  profileId: string;
  name: string;
  handle: string;
  avatarUrl: string | null;
  bio: string | null;
  mutualFollowCount: number;
}

export interface FollowersResponse {
  followers: FollowerDto[];
  totalCount: number;
  nextCursor: string | null;
  hasMore: boolean;
}

export interface FollowingResponse {
  following: FollowingDto[];
  totalCount: number;
  nextCursor: string | null;
  hasMore: boolean;
}

export interface SuggestedProfilesResponse {
  suggestions: ProfileSuggestion[];
}
```

#### Step 4.3: Core Hooks
- [ ] Create `features/social/hooks/useFollow.ts`:

```typescript
import { useState, useCallback } from "react";
import { socialApi } from "../../../services/socialApi";
import { useAuth } from "../../../hooks/useAuth";

export interface UseFollowResult {
  follow: (profileId: string) => Promise<void>;
  unfollow: (profileId: string) => Promise<void>;
  isLoading: boolean;
  error: string | null;
}

export function useFollow(): UseFollowResult {
  const { user } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const follow = useCallback(
    async (profileId: string): Promise<void> => {
      if (!user) {
        throw new Error("Must be authenticated to follow");
      }

      setIsLoading(true);
      setError(null);

      try {
        await socialApi.followUser(profileId);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to follow user";
        setError(errorMessage);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [user]
  );

  const unfollow = useCallback(
    async (profileId: string): Promise<void> => {
      if (!user) {
        throw new Error("Must be authenticated to unfollow");
      }

      setIsLoading(true);
      setError(null);

      try {
        await socialApi.unfollowUser(profileId);
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Failed to unfollow user";
        setError(errorMessage);
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [user]
  );

  return { follow, unfollow, isLoading, error };
}
```

- [ ] Create `features/social/hooks/useIsFollowing.ts`
- [ ] Create `features/social/hooks/useFollowers.ts`
- [ ] Create `features/social/hooks/useFollowing.ts`
- [ ] Create `features/social/hooks/useSuggestedProfiles.ts`

---

### Phase 5: Frontend Components

#### Step 5.1: FollowButton Component
- [ ] Create `features/social/components/FollowButton.tsx`:

```typescript
import { useState, useEffect } from "react";
import { useFollow } from "../hooks/useFollow";
import { socialApi } from "../../../services/socialApi";
import { useAuth } from "../../../hooks/useAuth";

export interface FollowButtonProps {
  profileId: string;
  onFollowChange?: (isFollowing: boolean) => void;
}

export function FollowButton({ profileId, onFollowChange }: FollowButtonProps): JSX.Element {
  const { user } = useAuth();
  const { follow, unfollow, isLoading } = useFollow();
  const [isFollowing, setIsFollowing] = useState(false);
  const [checkingStatus, setCheckingStatus] = useState(true);

  // Check if currently following on mount
  useEffect(() => {
    const checkFollowStatus = async () => {
      if (!user) {
        setCheckingStatus(false);
        return;
      }

      try {
        const result = await socialApi.isFollowing(profileId);
        setIsFollowing(result.isFollowing);
      } catch (err) {
        console.error("Failed to check follow status:", err);
      } finally {
        setCheckingStatus(false);
      }
    };

    void checkFollowStatus();
  }, [profileId, user]);

  const handleClick = async (): Promise<void> => {
    try {
      if (isFollowing) {
        await unfollow(profileId);
        setIsFollowing(false);
        onFollowChange?.(false);
      } else {
        await follow(profileId);
        setIsFollowing(true);
        onFollowChange?.(true);
      }
    } catch (err) {
      // Error already handled by hook
    }
  };

  if (checkingStatus) {
    return (
      <button
        type="button"
        disabled
        className="rounded-full px-4 py-1.5 text-sm font-semibold bg-surface-card2 text-text-tertiary cursor-not-allowed"
      >
        Loading...
      </button>
    );
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      disabled={isLoading}
      className={`rounded-full px-4 py-1.5 text-sm font-semibold transition-colors ${
        isFollowing
          ? "border border-border text-text-primary hover:bg-red-500/10 hover:border-red-500 hover:text-red-500"
          : "bg-text-primary text-surface-page hover:bg-text-secondary"
      } ${isLoading ? "opacity-50 cursor-not-allowed" : ""}`}
    >
      {isLoading ? "..." : isFollowing ? "Following" : "Follow"}
    </button>
  );
}
```

#### Step 5.2: Lists Components
- [ ] Create `features/social/components/FollowersList.tsx`:
  - Display followers with infinite scroll
  - Use `useFollowers` hook
  - Show avatar, name, handle, bio
  - Include follow button for each

- [ ] Create `features/social/components/FollowingList.tsx`:
  - Display following with infinite scroll
  - Use `useFollowing` hook
  - Similar structure to FollowersList

#### Step 5.3: Suggested Profiles
- [ ] Update `features/pulse/WhoToFollow.tsx`:
  - Use `useSuggestedProfiles` hook instead of mock data
  - Display mutual follow count ("Followed by X people you follow")
  - Wire up `FollowButton` component
  - Handle loading and error states

---

### Phase 6: Integration & Testing

#### Step 6.1: Update PulsePage Feed
- [ ] Update `features/pulse/hooks/useFeed.ts`:
  - Pass currentUserId to API (from useAuth)
  - Handle empty feed response
  - Update error handling

- [ ] Update `features/pulse/PulsePage.tsx`:
  - Show empty state: "Follow people to see their pulses"
  - Add CTA button to discover users
  - Link to "Who to Follow" suggestions

#### Step 6.2: Update PostCard (Optional)
- [ ] Show follow button on author in feed (if not following)
- [ ] Or link to author profile page where follow button exists

#### Step 6.3: Manual Testing Checklist
- [ ] **Follow a user** → See their pulses in feed
- [ ] **Unfollow** → Pulses disappear from feed
- [ ] **Cannot follow self** → Error prevented
- [ ] **Cannot follow twice** → Error prevented
- [ ] **Follower/following counts update** correctly after follow/unfollow
- [ ] **Suggestions show mutual follows** → "Followed by X people you follow"
- [ ] **Empty feed** shows correct message when not following anyone
- [ ] **Follow button states** → Loading, Follow, Following
- [ ] **Pagination works** → Infinite scroll for followers/following lists

---

## Performance & Scaling

### Current Scale Assumptions
- **MVP Target**: < 10,000 users, < 1,000 follows per user
- **Feed latency**: < 200ms for 99th percentile
- **Follow action**: < 100ms for write operation

### Known Bottlenecks & Future Optimizations

#### Bottleneck 1: Feed Query (Two-Query Approach)
**When it fails**: User follows > 1,000 people → `IN` clause becomes slow

**Solution**: Cache following IDs in Redis
- Key: `user:{id}:following`
- TTL: 1 hour
- Invalidate on follow/unfollow

**Implementation**:
```csharp
public interface IFollowingCacheService
{
    Task<List<Guid>> GetFollowingIdsAsync(Guid userId, CancellationToken ct);
    Task InvalidateCacheAsync(Guid userId, CancellationToken ct);
}
```

#### Bottleneck 2: Follower/Following Counts
**When it fails**: Profiles with > 10K followers → `COUNT(*)` becomes slow

**Solution**: Denormalize counts to Profile table
- Add columns: `FollowerCount`, `FollowingCount` to Profile
- Update via event handlers or background sync job

**Implementation**:
```csharp
// Add to Profile entity
public int FollowerCount { get; private set; }
public int FollowingCount { get; private set; }

// Increment in FollowUserCommandHandler
profile.IncrementFollowerCount();

// Background job to fix drift
public class SyncFollowCountsJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // Recount and update Profile.FollowerCount/FollowingCount
    }
}
```

#### Bottleneck 3: Suggested Profiles (2-Hop Query)
**When it fails**: Users with > 5,000 follows → `JOIN` becomes expensive

**Solution**: Pre-compute suggestions
- Nightly background job calculates top 50 suggestions per user
- Store in cache or dedicated `ProfileSuggestions` table
- Refresh daily or on-demand

---

## Testing Strategy

### Unit Tests
- [ ] `Follow.Create()` factory method
- [ ] Exception throwing (self-follow, duplicate follow)
- [ ] `FollowUserValidator` validation logic
- [ ] `UnfollowUserValidator` validation logic

### Integration Tests
- [ ] `FollowRepository.CreateFollow()` + `FindFollowAsync()`
- [ ] `FollowRepository.GetFollowersAsync()` with pagination
- [ ] `FollowRepository.GetFollowingAsync()` with pagination
- [ ] `FollowRepository.GetSuggestedProfilesAsync()` 2-hop algorithm
- [ ] `PulseRepository.GetFeedAsync()` with follow filtering
- [ ] Empty feed when not following anyone

### API Tests
- [ ] `POST /api/social/{id}/follow` (success, duplicate, self-follow)
- [ ] `DELETE /api/social/{id}/follow` (success, not following)
- [ ] `GET /api/social/{id}/followers` with pagination
- [ ] `GET /api/social/{id}/following` with pagination
- [ ] `GET /api/social/{id}/is-following` returns correct boolean
- [ ] `GET /api/social/suggestions` returns sorted by mutual follows

### Frontend Tests
- [ ] `FollowButton` renders correct state (Follow/Following)
- [ ] `FollowButton` optimistic updates
- [ ] `useFeed` handles empty feed response
- [ ] `WhoToFollow` displays suggestions correctly

---

## Success Criteria

✅ Users can follow/unfollow other users
✅ Feed shows only pulses from followed users (+ own pulses)
✅ Empty feed shows helpful message: "Follow people to see their pulses"
✅ Follower/following counts display correctly
✅ "Who to Follow" shows suggestions based on mutual follows
✅ Cannot follow yourself
✅ Cannot follow the same user twice
✅ No performance degradation (feed < 200ms, follow/unfollow < 100ms)
✅ All existing tests pass
✅ TypeScript strict mode passes
✅ Backend builds successfully
✅ Frontend builds successfully

---

## Estimated Implementation Timeline

| Phase | Description | Estimated Time |
|-------|-------------|----------------|
| Phase 1 | Backend Foundation (Domain + DB) | 2-3 hours |
| Phase 2 | Backend Commands & Queries | 3-4 hours |
| Phase 3 | Backend API Endpoints | 1-2 hours |
| Phase 4 | Frontend Infrastructure | 1-2 hours |
| Phase 5 | Frontend Components | 3-4 hours |
| Phase 6 | Integration & Testing | 2-3 hours |
| **Total** | **End-to-end implementation** | **12-18 hours** |

---

## Research Sources

This plan is based on:
- [SQL Server Graph Processing - Microsoft Learn](https://learn.microsoft.com/en-us/sql/relational-databases/graphs/sql-graph-overview?view=sql-server-ver17)
- [SQL Graph Pattern Matching - SQL Authority](https://blog.sqlauthority.com/2023/10/25/sql-server-exploring-sql-graph-pattern-matching-with-match/)
- [SQL Graph Queries 2025 - SqlCheat](https://sqlcheat.com/blog/sql-graph-queries-2025/)
- [Followers Database Design - Stack Overflow](https://stackoverflow.com/questions/4895809/database-design-for-followers-and-followings)
- [Followers System Schema - Medium](https://medium.com/@oluwatolaodunlami19/designing-an-optimal-database-schema-for-a-followers-following-system-in-a-social-media-app-best-2f2cb5ce86ac)
- [Twitter Architecture Design - Hayk Simonyan](https://hayksimonyan.substack.com/p/system-design-interview-design-twitter)
- [System Design Twitter - System Design School](https://systemdesignschool.io/problems/twitter/solution)
- [Denormalization Best Practices - Medium](https://medium.com/@oluwatolaodunlami19/designing-an-optimal-database-schema-for-a-followers-following-system-in-a-social-media-app-best-2f2cb5ce86ac)
- [Denormalized Data Performance - Zenduty](https://zenduty.com/blog/data-denormalization/)

---

## Notes for Implementation

- **EF Core Only**: All database changes via migrations, no custom SQL
- **LINQ Queries**: All repository methods use LINQ, no raw SQL
- **Follow Patterns**: Strictly follow existing codebase conventions (Kommand, validators, repositories)
- **Indexes in Config**: All indexes defined in `FollowConfiguration.cs`
- **No Graph DB**: SQL Server with proper indexes is sufficient for 2-hop queries
- **Start Simple**: No denormalization initially, add when needed based on metrics
- **TypeScript Strict**: All frontend code must pass strict type checking
- **Test Coverage**: Write tests as you go, don't defer to end

---

**Last Updated**: 2025-11-28
