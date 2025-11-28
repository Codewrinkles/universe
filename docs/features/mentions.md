# @Mentions Feature - Technical Implementation Plan

> **Status**: Not implemented
> **Created**: 2025-11-28
> **Feature**: Allow users to mention other users in pulses with @handle syntax and autocomplete

---

## Overview

This document outlines the complete implementation plan for adding @mentions to pulses with autocomplete functionality. Users will be able to type `@` followed by a handle to mention other users, with real-time autocomplete suggestions appearing as they type.

### User Experience Flow

1. User types `@` in the composer
2. Autocomplete dropdown appears showing matching handles
3. User types more characters to filter suggestions
4. User navigates with arrow keys and selects with Enter (or clicks)
5. Mention is inserted as `@handle` in the content
6. When viewing pulses, `@handle` is rendered as clickable link to profile
7. Mentioned users can receive notifications (future feature)

---

## Phase 1: Backend Implementation

### Step 1.1: Create PulseMention Entity

**File**: `apps/backend/src/Codewrinkles.Domain/Pulse/PulseMention.cs`

```csharp
namespace Codewrinkles.Domain.Pulse;

/// <summary>
/// Represents a mention of a user in a pulse
/// Stores mention relationships for notifications and analytics
/// </summary>
public sealed class PulseMention
{
    public Guid PulseId { get; private set; }
    public Guid ProfileId { get; private set; }
    public string Handle { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Pulse Pulse { get; private set; }
    public Profile MentionedProfile { get; private set; }

    // Private parameterless constructor for EF Core
#pragma warning disable CS8618
    private PulseMention() { }
#pragma warning restore CS8618

    // Factory method
    public static PulseMention Create(Guid pulseId, Guid profileId, string handle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handle);

        return new PulseMention
        {
            PulseId = pulseId,
            ProfileId = profileId,
            Handle = handle.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

**Why**:
- Stores mention relationships for future notifications
- Denormalizes handle for quick access
- Composite key prevents duplicate mentions

---

### Step 1.2: Add EF Core Configuration

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Pulse/PulseMentionConfiguration.cs`

```csharp
using Codewrinkles.Domain.Pulse;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Codewrinkles.Infrastructure.Persistence.Configurations.Pulse;

public sealed class PulseMentionConfiguration : IEntityTypeConfiguration<PulseMention>
{
    public void Configure(EntityTypeBuilder<PulseMention> builder)
    {
        builder.ToTable("PulseMentions", "pulse");

        // Composite primary key
        builder.HasKey(m => new { m.PulseId, m.ProfileId });

        // Properties
        builder.Property(m => m.Handle)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(m => m.Pulse)
            .WithMany()
            .HasForeignKey(m => m.PulseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.MentionedProfile)
            .WithMany()
            .HasForeignKey(m => m.ProfileId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete when profile is deleted

        // Indexes
        builder.HasIndex(m => m.Handle);
        builder.HasIndex(m => m.CreatedAt);
    }
}
```

**Why**:
- Composite key (PulseId, ProfileId) prevents duplicate mentions
- Cascade delete when pulse is deleted
- Restrict delete when profile is deleted (mentions stay for context)
- Index on handle for autocomplete queries

---

### Step 1.3: Update ApplicationDbContext

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/ApplicationDbContext.cs`

Add DbSet:
```csharp
public DbSet<PulseMention> PulseMentions => Set<PulseMention>();
```

Apply configuration in `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new PulseMentionConfiguration());
```

---

### Step 1.4: Create Migration

**Terminal**:
```bash
cd apps/backend/src/Codewrinkles.API
dotnet ef migrations add AddPulseMentions --project ../Codewrinkles.Infrastructure
dotnet ef database update
```

**Expected Migration** (auto-generated):
```csharp
migrationBuilder.CreateTable(
    name: "PulseMentions",
    schema: "pulse",
    columns: table => new
    {
        PulseId = table.Column<Guid>(nullable: false),
        ProfileId = table.Column<Guid>(nullable: false),
        Handle = table.Column<string>(maxLength: 30, nullable: false),
        CreatedAt = table.Column<DateTime>(nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_PulseMentions", x => new { x.PulseId, x.ProfileId });
        table.ForeignKey(
            name: "FK_PulseMentions_Pulses_PulseId",
            column: x => x.PulseId,
            principalSchema: "pulse",
            principalTable: "Pulses",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
        table.ForeignKey(
            name: "FK_PulseMentions_Profiles_ProfileId",
            column: x => x.ProfileId,
            principalSchema: "identity",
            principalTable: "Profiles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    });

migrationBuilder.CreateIndex(
    name: "IX_PulseMentions_Handle",
    schema: "pulse",
    table: "PulseMentions",
    column: "Handle");

migrationBuilder.CreateIndex(
    name: "IX_PulseMentions_CreatedAt",
    schema: "pulse",
    table: "PulseMentions",
    column: "CreatedAt");
```

---

### Step 1.5: Add SearchHandles Query (for autocomplete)

**File**: `apps/backend/src/Codewrinkles.Application/Users/SearchHandles.cs`

```csharp
using Codewrinkles.Application.Common;
using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Identity;

namespace Codewrinkles.Application.Users;

// Query
public sealed record SearchHandlesQuery(string SearchTerm, int Limit = 10) : IQuery<SearchHandlesResult>;

// Result
public sealed record SearchHandlesResult(List<HandleSearchDto> Handles);

public sealed record HandleSearchDto(
    Guid ProfileId,
    string Handle,
    string Name,
    string? AvatarUrl
);

// Handler
public sealed class SearchHandlesQueryHandler : IQueryHandler<SearchHandlesQuery, SearchHandlesResult>
{
    private readonly IProfileRepository _profileRepository;

    public SearchHandlesQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<SearchHandlesResult> Handle(SearchHandlesQuery request, CancellationToken cancellationToken)
    {
        // Search handles that start with the search term (case-insensitive)
        var profiles = await _profileRepository.SearchByHandleAsync(
            request.SearchTerm,
            request.Limit,
            cancellationToken);

        var handles = profiles
            .Select(p => new HandleSearchDto(
                p.Id,
                p.Handle!,
                p.Name,
                p.AvatarUrl))
            .ToList();

        return new SearchHandlesResult(handles);
    }
}
```

**Why**:
- Simple query to search handles by prefix
- Returns minimal data needed for autocomplete (handle, name, avatar)
- Limits results to prevent large responses

---

### Step 1.6: Add SearchByHandleAsync to IProfileRepository

**File**: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IProfileRepository.cs`

Add method:
```csharp
Task<IReadOnlyList<Profile>> SearchByHandleAsync(
    string searchTerm,
    int limit,
    CancellationToken cancellationToken = default);
```

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/ProfileRepository.cs`

Implement method:
```csharp
public async Task<IReadOnlyList<Profile>> SearchByHandleAsync(
    string searchTerm,
    int limit,
    CancellationToken cancellationToken = default)
{
    var normalizedSearch = searchTerm.Trim().ToLowerInvariant();

    var profiles = await _profiles
        .AsNoTracking()
        .Where(p => p.Handle != null && p.Handle.StartsWith(normalizedSearch))
        .OrderBy(p => p.Handle)
        .Take(limit)
        .ToListAsync(cancellationToken);

    return profiles;
}
```

**Why**:
- Case-insensitive prefix search
- Orders alphabetically for consistent UX
- Limits results to prevent performance issues

---

### Step 1.7: Add Handle Search Endpoint

**File**: `apps/backend/src/Codewrinkles.API/Modules/Identity/ProfileEndpoints.cs`

Add endpoint:
```csharp
public static void MapSearchHandles(IEndpointRouteBuilder app)
{
    app.MapGet("/api/profile/search", async (
        [FromQuery] string q,
        [FromQuery] int limit,
        ISender sender,
        CancellationToken cancellationToken) =>
    {
        // Validate query parameter
        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.BadRequest(new { error = "Search term is required" });
        }

        // Enforce limit bounds
        var effectiveLimit = Math.Clamp(limit > 0 ? limit : 10, 1, 20);

        var query = new SearchHandlesQuery(q, effectiveLimit);
        var result = await sender.Send(query, cancellationToken);

        return Results.Ok(new
        {
            handles = result.Handles.Select(h => new
            {
                profileId = h.ProfileId,
                handle = h.Handle,
                name = h.Name,
                avatarUrl = h.AvatarUrl
            })
        });
    })
    .WithName("SearchHandles")
    .WithTags("Profile");
}
```

Register in `Program.cs`:
```csharp
ProfileEndpoints.MapSearchHandles(app);
```

**Why**:
- Public endpoint (no auth required for handle search)
- Query parameter `q` for search term
- Configurable limit with bounds (1-20)

---

### Step 1.8: Extract Mentions from Pulse Content

**File**: `apps/backend/src/Codewrinkles.Application/Pulse/CreatePulse.cs`

Add mention extraction logic to handler:

```csharp
public sealed class CreatePulseCommandHandler : ICommandHandler<CreatePulseCommand, CreatePulseResult>
{
    private readonly IPulseRepository _pulseRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IUnitOfWork _unitOfWork;

    // ... constructor

    public async Task<CreatePulseResult> Handle(CreatePulseCommand request, CancellationToken cancellationToken)
    {
        // ... existing validation and pulse creation code

        // Extract mentions from content
        var mentionedHandles = ExtractMentions(request.Content);

        // Validate and resolve mentioned handles to profile IDs
        var mentions = await ResolveHandlesToProfiles(mentionedHandles, cancellationToken);

        // Create pulse (existing code)
        var pulse = Pulse.Create(
            request.UserId,
            request.Content,
            request.ParentPulseId,
            request.RepulsedPulseId);

        _pulseRepository.Create(pulse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create mention records
        foreach (var mention in mentions)
        {
            var pulseMention = PulseMention.Create(pulse.Id, mention.ProfileId, mention.Handle);
            _pulseRepository.CreateMention(pulseMention);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ... return result
    }

    private static List<string> ExtractMentions(string content)
    {
        // Regex to match @handle (alphanumeric + underscore, 3-30 chars)
        var mentionPattern = @"@(\w{3,30})";
        var matches = Regex.Matches(content, mentionPattern);

        return matches
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    private async Task<List<(Guid ProfileId, string Handle)>> ResolveHandlesToProfiles(
        List<string> handles,
        CancellationToken cancellationToken)
    {
        if (handles.Count == 0)
        {
            return [];
        }

        var profiles = await _profileRepository.FindByHandlesAsync(handles, cancellationToken);

        return profiles
            .Select(p => (p.Id, p.Handle!))
            .ToList();
    }
}
```

**Why**:
- Extract mentions using regex after pulse is created
- Only create mention records for valid handles
- Ignore mentions with non-existent handles (graceful degradation)

---

### Step 1.9: Add FindByHandlesAsync to Repository

**File**: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IProfileRepository.cs`

Add method:
```csharp
Task<IReadOnlyList<Profile>> FindByHandlesAsync(
    IEnumerable<string> handles,
    CancellationToken cancellationToken = default);
```

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/ProfileRepository.cs`

Implement method:
```csharp
public async Task<IReadOnlyList<Profile>> FindByHandlesAsync(
    IEnumerable<string> handles,
    CancellationToken cancellationToken = default)
{
    var normalizedHandles = handles
        .Select(h => h.Trim().ToLowerInvariant())
        .ToList();

    var profiles = await _profiles
        .AsNoTracking()
        .Where(p => p.Handle != null && normalizedHandles.Contains(p.Handle))
        .ToListAsync(cancellationToken);

    return profiles;
}
```

---

### Step 1.10: Add CreateMention to IPulseRepository

**File**: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IPulseRepository.cs`

Add method:
```csharp
void CreateMention(PulseMention mention);
```

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/PulseRepository.cs`

Add field:
```csharp
private readonly DbSet<PulseMention> _mentions;
```

Initialize in constructor:
```csharp
_mentions = context.Set<PulseMention>();
```

Implement method:
```csharp
public void CreateMention(PulseMention mention)
{
    _mentions.Add(mention);
}
```

---

### Step 1.11: Include Mentions in Pulse DTOs

**File**: `apps/backend/src/Codewrinkles.Application/Pulse/GetFeed.cs`

Update `PulseDto`:
```csharp
public sealed record PulseDto(
    Guid Id,
    string Content,
    AuthorDto Author,
    DateTime CreatedAt,
    int LikesCount,
    int RepliesCount,
    int RepulsesCount,
    bool? IsLikedByCurrentUser,
    bool? IsFollowingAuthor,
    Guid? ParentPulseId,
    RepulsedPulseDto? RepulsedPulse,
    PulseImageDto? Image,
    List<MentionDto> Mentions // ADD THIS
);

public sealed record MentionDto(
    Guid ProfileId,
    string Handle
);
```

Update query handler to load mentions:
```csharp
// In GetFeedQueryHandler.Handle method, after loading pulses:
var pulseIds = pulses.Select(p => p.Id).ToList();

// Load mentions for all pulses in batch
var mentions = await _pulseRepository.GetMentionsForPulsesAsync(pulseIds, cancellationToken);
var mentionsByPulse = mentions.GroupBy(m => m.PulseId).ToDictionary(g => g.Key, g => g.ToList());

// When mapping to DTO:
var mentionsDto = mentionsByPulse.TryGetValue(pulse.Id, out var pulseMentions)
    ? pulseMentions.Select(m => new MentionDto(m.ProfileId, m.Handle)).ToList()
    : [];
```

---

### Step 1.12: Add GetMentionsForPulsesAsync to Repository

**File**: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IPulseRepository.cs`

Add method:
```csharp
Task<IReadOnlyList<PulseMention>> GetMentionsForPulsesAsync(
    IEnumerable<Guid> pulseIds,
    CancellationToken cancellationToken = default);
```

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/PulseRepository.cs`

Implement method:
```csharp
public async Task<IReadOnlyList<PulseMention>> GetMentionsForPulsesAsync(
    IEnumerable<Guid> pulseIds,
    CancellationToken cancellationToken = default)
{
    var pulseIdList = pulseIds.ToList();

    if (pulseIdList.Count == 0)
    {
        return [];
    }

    var mentions = await _mentions
        .AsNoTracking()
        .Where(m => pulseIdList.Contains(m.PulseId))
        .ToListAsync(cancellationToken);

    return mentions;
}
```

**Why**:
- Batch load mentions for all pulses in feed (prevents N+1 queries)
- Returns mentions grouped by pulse

---

## Phase 2: Frontend Implementation

### Step 2.1: Add API Client Methods

**File**: `apps/frontend/src/config.ts`

Add endpoint:
```typescript
export const config = {
  api: {
    endpoints: {
      // ... existing endpoints
      searchHandles: (query: string, limit: number = 10) =>
        `${config.api.baseUrl}/api/profile/search?q=${encodeURIComponent(query)}&limit=${limit}`,
    }
  }
};
```

---

### Step 2.2: Update Types

**File**: `apps/frontend/src/types.ts`

Add mention types:
```typescript
export interface Mention {
  profileId: string;
  handle: string;
}

export interface Post {
  // ... existing fields
  mentions: Mention[]; // ADD THIS
}

export interface HandleSearchResult {
  profileId: string;
  handle: string;
  name: string;
  avatarUrl: string | null;
}
```

---

### Step 2.3: Create useHandleSearch Hook

**File**: `apps/frontend/src/features/pulse/hooks/useHandleSearch.ts`

```typescript
import { useState, useEffect, useCallback } from "react";
import { config } from "../../../config";
import type { HandleSearchResult } from "../../../types";

interface UseHandleSearchResult {
  results: HandleSearchResult[];
  isLoading: boolean;
  error: string | null;
  search: (query: string) => void;
  clear: () => void;
}

export function useHandleSearch(): UseHandleSearchResult {
  const [results, setResults] = useState<HandleSearchResult[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const search = useCallback(async (query: string): Promise<void> => {
    // Require at least 1 character
    if (query.length === 0) {
      setResults([]);
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch(config.api.endpoints.searchHandles(query, 10));

      if (!response.ok) {
        throw new Error("Failed to search handles");
      }

      const data = await response.json();
      setResults(data.handles || []);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to search handles");
      setResults([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const clear = useCallback((): void => {
    setResults([]);
    setError(null);
  }, []);

  return {
    results,
    isLoading,
    error,
    search,
    clear,
  };
}
```

---

### Step 2.4: Create MentionAutocomplete Component

**File**: `apps/frontend/src/features/pulse/MentionAutocomplete.tsx`

```typescript
import { useEffect, useRef } from "react";
import { Link } from "react-router-dom";
import { config } from "../../config";
import type { HandleSearchResult } from "../../types";

export interface MentionAutocompleteProps {
  results: HandleSearchResult[];
  selectedIndex: number;
  onSelect: (handle: string) => void;
  position: { top: number; left: number } | null;
}

export function MentionAutocomplete({
  results,
  selectedIndex,
  onSelect,
  position,
}: MentionAutocompleteProps): JSX.Element | null {
  const listRef = useRef<HTMLDivElement>(null);

  // Scroll selected item into view
  useEffect(() => {
    if (listRef.current && selectedIndex >= 0) {
      const selectedElement = listRef.current.children[selectedIndex] as HTMLElement;
      selectedElement?.scrollIntoView({ block: "nearest" });
    }
  }, [selectedIndex]);

  if (results.length === 0 || !position) {
    return null;
  }

  return (
    <div
      ref={listRef}
      className="fixed z-50 w-64 max-h-64 overflow-y-auto bg-surface-card1 border border-border rounded-lg shadow-lg"
      style={{ top: position.top, left: position.left }}
    >
      {results.map((result, index) => (
        <button
          key={result.profileId}
          type="button"
          onClick={() => onSelect(result.handle)}
          className={`w-full flex items-center gap-3 px-3 py-2 text-left hover:bg-surface-card2 transition-colors ${
            index === selectedIndex ? "bg-surface-card2" : ""
          }`}
        >
          {/* Avatar */}
          <div className="h-8 w-8 rounded-full overflow-hidden border border-border flex-shrink-0">
            {result.avatarUrl ? (
              <img
                src={`${config.api.baseUrl}${result.avatarUrl}`}
                alt={result.name}
                className="h-full w-full object-cover"
              />
            ) : (
              <div className="h-full w-full bg-surface-card2 flex items-center justify-center">
                <span className="text-sm text-text-tertiary">
                  {result.name.charAt(0).toUpperCase()}
                </span>
              </div>
            )}
          </div>

          {/* Name and Handle */}
          <div className="flex-1 min-w-0">
            <div className="text-sm font-medium text-text-primary truncate">
              {result.name}
            </div>
            <div className="text-xs text-text-secondary truncate">
              @{result.handle}
            </div>
          </div>
        </button>
      ))}
    </div>
  );
}
```

**Why**:
- Fixed positioning to appear near cursor
- Keyboard-accessible with selected index highlighting
- Shows avatar, name, and handle for context
- Scrolls selected item into view

---

### Step 2.5: Update Composer with Mention Detection

**File**: `apps/frontend/src/features/pulse/Composer.tsx`

Add mention detection and autocomplete:

```typescript
import { useState, useRef, useEffect } from "react";
import { useHandleSearch } from "./hooks/useHandleSearch";
import { MentionAutocomplete } from "./MentionAutocomplete";

export function Composer({
  value,
  onChange,
  maxChars,
  isOverLimit,
  charsLeft,
  onSubmit,
  isSubmitting,
  selectedImage,
  onImageSelect,
}: ComposerProps): JSX.Element {
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const { results, isLoading, search, clear } = useHandleSearch();
  const [showAutocomplete, setShowAutocomplete] = useState(false);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [autocompletePosition, setAutocompletePosition] = useState<{ top: number; left: number } | null>(null);
  const [mentionStartIndex, setMentionStartIndex] = useState<number | null>(null);

  // Detect @ character and trigger autocomplete
  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>): void => {
    const newValue = e.target.value;
    onChange(newValue);

    const cursorPosition = e.target.selectionStart;

    // Find @ symbol before cursor
    const textBeforeCursor = newValue.slice(0, cursorPosition);
    const lastAtIndex = textBeforeCursor.lastIndexOf("@");

    if (lastAtIndex !== -1) {
      const textAfterAt = textBeforeCursor.slice(lastAtIndex + 1);

      // Check if we're still typing the mention (no spaces after @)
      if (!textAfterAt.includes(" ") && textAfterAt.length <= 30) {
        setMentionStartIndex(lastAtIndex);
        setShowAutocomplete(true);
        setSelectedIndex(0);

        // Calculate autocomplete position
        if (textareaRef.current) {
          const rect = textareaRef.current.getBoundingClientRect();
          setAutocompletePosition({
            top: rect.bottom + window.scrollY + 4,
            left: rect.left + window.scrollX,
          });
        }

        // Search for handles
        search(textAfterAt);
      } else {
        setShowAutocomplete(false);
        clear();
      }
    } else {
      setShowAutocomplete(false);
      clear();
    }
  };

  // Handle keyboard navigation in autocomplete
  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>): void => {
    if (!showAutocomplete || results.length === 0) {
      return;
    }

    if (e.key === "ArrowDown") {
      e.preventDefault();
      setSelectedIndex((prev) => (prev + 1) % results.length);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setSelectedIndex((prev) => (prev - 1 + results.length) % results.length);
    } else if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      insertMention(results[selectedIndex].handle);
    } else if (e.key === "Escape") {
      e.preventDefault();
      setShowAutocomplete(false);
      clear();
    }
  };

  // Insert selected mention
  const insertMention = (handle: string): void => {
    if (mentionStartIndex === null) return;

    const before = value.slice(0, mentionStartIndex);
    const after = value.slice(textareaRef.current?.selectionStart || value.length);
    const newValue = `${before}@${handle} ${after}`;

    onChange(newValue);
    setShowAutocomplete(false);
    clear();

    // Focus back on textarea and move cursor after mention
    setTimeout(() => {
      if (textareaRef.current) {
        const newCursorPos = mentionStartIndex + handle.length + 2; // +2 for @ and space
        textareaRef.current.focus();
        textareaRef.current.setSelectionRange(newCursorPos, newCursorPos);
      }
    }, 0);
  };

  return (
    <div className="relative">
      <textarea
        ref={textareaRef}
        value={value}
        onChange={handleChange}
        onKeyDown={handleKeyDown}
        placeholder="What's on your mind?"
        className="w-full min-h-[100px] bg-transparent text-text-primary text-sm resize-none focus:outline-none"
        disabled={isSubmitting}
      />

      {/* ... rest of composer (character count, image preview, submit button) */}

      {/* Autocomplete */}
      {showAutocomplete && (
        <MentionAutocomplete
          results={results}
          selectedIndex={selectedIndex}
          onSelect={insertMention}
          position={autocompletePosition}
        />
      )}
    </div>
  );
}
```

**Why**:
- Detects `@` and triggers autocomplete
- Debounces search as user types
- Keyboard navigation (up/down/enter/escape)
- Inserts mention and repositions cursor

---

### Step 2.6: Parse and Render Mentions in PostCard

**File**: `apps/frontend/src/features/pulse/utils/parseMentions.tsx`

```typescript
import { Link } from "react-router-dom";
import type { Mention } from "../../../types";

/**
 * Parse pulse content and replace @mentions with clickable links
 */
export function parseContentWithMentions(content: string, mentions: Mention[]): JSX.Element {
  // Create a map of handles to profile IDs
  const mentionMap = new Map(
    mentions.map((m) => [m.handle.toLowerCase(), m.profileId])
  );

  // Regex to match @handle (same as backend)
  const mentionPattern = /@(\w{3,30})/g;

  const parts: (string | JSX.Element)[] = [];
  let lastIndex = 0;
  let match;

  while ((match = mentionPattern.exec(content)) !== null) {
    const handle = match[1].toLowerCase();
    const profileId = mentionMap.get(handle);

    // Add text before mention
    if (match.index > lastIndex) {
      parts.push(content.slice(lastIndex, match.index));
    }

    // Add mention link if valid, otherwise plain text
    if (profileId) {
      parts.push(
        <Link
          key={`${profileId}-${match.index}`}
          to={`/pulse/u/${handle}`}
          onClick={(e) => e.stopPropagation()}
          className="text-brand-soft hover:text-brand transition-colors font-medium"
        >
          @{handle}
        </Link>
      );
    } else {
      parts.push(`@${handle}`);
    }

    lastIndex = match.index + match[0].length;
  }

  // Add remaining text
  if (lastIndex < content.length) {
    parts.push(content.slice(lastIndex));
  }

  return <>{parts}</>;
}
```

---

### Step 2.7: Update PostCard to Use Mention Parsing

**File**: `apps/frontend/src/features/pulse/PostCard.tsx`

Import and use the parser:

```typescript
import { parseContentWithMentions } from "./utils/parseMentions";

// In PostCard component, replace:
// <p className="text-sm text-text-primary whitespace-pre-wrap break-words">{content}</p>

// With:
<div className="text-sm text-text-primary whitespace-pre-wrap break-words">
  {parseContentWithMentions(post.content, post.mentions)}
</div>
```

**Why**:
- Renders mentions as clickable links
- Uses brand color for visual distinction
- Falls back to plain text if handle not in mentions list

---

## Phase 3: Testing & Refinement

### Step 3.1: Backend Testing

**Manual Testing Steps**:
1. Create pulse with mention: `"Hey @danp check this out"`
2. Verify mention is stored in `PulseMentions` table
3. Verify pulse DTO includes mentions
4. Test handle search endpoint: `/api/profile/search?q=dan`
5. Test with invalid handles (should be ignored)
6. Test with multiple mentions in same pulse
7. Test with duplicate mentions (should dedupe)

**Edge Cases**:
- Mention at start of content: `"@danp hello"`
- Mention at end of content: `"hello @danp"`
- Multiple mentions: `"@danp and @alexandra"`
- Invalid handle: `"@nonexistentuser"` (should be ignored)
- Self-mention: `"@myownhandle"` (allowed, but could be filtered)

---

### Step 3.2: Frontend Testing

**Manual Testing Steps**:
1. Type `@` in composer and verify autocomplete appears
2. Type more characters and verify results filter
3. Navigate with arrow keys and verify selection
4. Press Enter and verify mention is inserted
5. Press Escape and verify autocomplete closes
6. View pulse with mentions and verify links work
7. Click mention link and verify profile loads

**Edge Cases**:
- Typing `@` at end of max character limit
- Typing `@` with no matching handles
- Selecting mention near end of textarea
- Multiple `@` symbols in same content
- Fast typing (debounce behavior)

---

### Step 3.3: Performance Optimization

**Backend**:
- ✅ Already batch-loading mentions (prevents N+1)
- ✅ Already indexing handle for search
- ✅ Limiting search results to 10-20

**Frontend**:
- Add debounce to search (wait 200ms after typing stops)
- Cache search results to avoid duplicate API calls
- Lazy load MentionAutocomplete (code splitting)

---

### Step 3.4: Polish & UX Improvements

**Autocomplete**:
- Add loading indicator while searching
- Show "No results" message when search returns empty
- Highlight matching characters in results
- Add hover effect on autocomplete items

**Rendering**:
- Consider showing mention as bold instead of color
- Add tooltip with full name on mention hover
- Consider "verified" badge for verified users (future)

**Composer**:
- Add visual feedback when mention is inserted
- Consider showing mention count in composer footer
- Add ability to remove mention by backspacing

---

## Phase 4: Future Enhancements (Not in Initial Implementation)

### Notifications
- Notify users when they're mentioned
- Add "Mentions" tab in notifications page
- Badge count for unread mentions

### Analytics
- Track mention frequency per user
- Show "most mentioned" users
- Mention trends over time

### Privacy
- Allow users to disable mentions
- Require follow to mention someone
- Report abusive mentions

### Advanced Features
- Mention suggestions based on following
- Recent mentions quick-list
- Mention groups (e.g., `@team`)

---

## Implementation Checklist

### Backend
- [ ] Create `PulseMention` entity
- [ ] Add EF Core configuration
- [ ] Update `ApplicationDbContext`
- [ ] Create and run migration
- [ ] Add `SearchHandles` query and handler
- [ ] Add `SearchByHandleAsync` to repository
- [ ] Add handle search endpoint
- [ ] Extract mentions in `CreatePulse` handler
- [ ] Add `FindByHandlesAsync` to repository
- [ ] Add `CreateMention` to repository
- [ ] Include mentions in pulse DTOs
- [ ] Add `GetMentionsForPulsesAsync` to repository
- [ ] Test all backend endpoints

### Frontend
- [ ] Add API endpoint to config
- [ ] Update types (Mention, HandleSearchResult)
- [ ] Create `useHandleSearch` hook
- [ ] Create `MentionAutocomplete` component
- [ ] Update `Composer` with mention detection
- [ ] Create `parseMentions` utility
- [ ] Update `PostCard` to render mentions
- [ ] Test autocomplete keyboard navigation
- [ ] Test mention rendering
- [ ] Add loading states
- [ ] Add error handling

### Testing
- [ ] Test backend mention extraction
- [ ] Test handle search with various queries
- [ ] Test mention storage and retrieval
- [ ] Test frontend autocomplete UX
- [ ] Test keyboard navigation
- [ ] Test mention link rendering
- [ ] Test edge cases (see Phase 3)
- [ ] Performance test with many mentions

---

## Estimated Effort

- **Backend**: 3-4 hours
  - Entity + config + migration: 1 hour
  - Search endpoint: 30 minutes
  - Mention extraction: 1 hour
  - Testing: 1 hour

- **Frontend**: 4-5 hours
  - Hook + API: 30 minutes
  - Autocomplete component: 2 hours
  - Composer integration: 1.5 hours
  - Mention rendering: 30 minutes
  - Testing + polish: 1 hour

- **Total**: 7-9 hours

---

## Questions to Resolve Before Implementation

1. **Self-mentions**: Should users be able to mention themselves?
2. **Duplicate mentions**: If same user mentioned twice, create one or two records?
3. **Invalid handles**: Silent ignore or show warning in composer?
4. **Search visibility**: Should handle search require authentication?
5. **Notification scope**: Immediate implementation or defer to Phase 4?
6. **Debounce timing**: How long to wait before searching (200ms? 300ms?)?
7. **Autocomplete styling**: Match existing dropdown patterns or custom design?

---

## References

- Twitter/X mentions UX: https://twitter.com
- Regex for mentions: `@(\w{3,30})`
- EF Core composite keys: https://learn.microsoft.com/en-us/ef/core/modeling/keys
- React textarea cursor position: https://developer.mozilla.org/en-US/docs/Web/API/HTMLTextAreaElement/selectionStart

---

**Document Status**: Draft - Ready for review
**Next Steps**: Review with team, answer questions, begin implementation
