# DateTimeOffset Migration Plan

> **Purpose**: Complete migration from `DateTime` to `DateTimeOffset` across the entire Codewrinkles application to fix timestamp handling issues.

---

## Problem Statement

**Symptom**: Pulses created "just now" display as "2h ago" for users in UTC+2 timezone.

**Root Cause Chain**:
1. Backend creates timestamps with `DateTime.UtcNow` (Kind = UTC) ✓
2. SQL Server `datetime2` columns do NOT store timezone information
3. EF Core reads values back with `Kind = DateTimeKind.Unspecified`
4. System.Text.Json serializes Unspecified without 'Z' suffix: `"2025-12-07T10:30:00"`
5. JavaScript `new Date("2025-12-07T10:30:00")` parses as LOCAL time
6. Time difference calculation uses local time, causing offset equal to user's timezone

**Solution**: Migrate to `DateTimeOffset` which:
- Stores offset information in SQL Server (`datetimeoffset` column type)
- Always serializes with offset: `"2025-12-07T10:30:00+00:00"`
- JavaScript correctly parses timezone-aware strings

---

## Migration Scope

### Database Changes
- **Column Type**: `datetime2` → `datetimeoffset`
- **Default Value SQL**: `GETUTCDATE()` → `SYSDATETIMEOFFSET()`
- **Total Columns**: 29 timestamp columns across 13 tables

### Backend Changes
- **Domain Entities**: 13 files
- **EF Core Configurations**: 15 files
- **Application DTOs**: 6 files with DateTime properties
- **Handlers/Services**: ~22 files using DateTime
- **Repository Interfaces**: 4 files with DateTime parameters

### Frontend Changes
- **Type Definitions**: 1 file (`types.ts`) - no changes needed (strings)
- **Time Utilities**: 1 file (`timeUtils.ts`) - no changes needed if backend sends proper ISO strings

---

## Phase 1: Domain Entity Changes

### 1.1 Identity Module

#### File: `apps/backend/src/Codewrinkles.Domain/Identity/Identity.cs`

**Properties to change** (lines 20-23):
```
DateTime? LockedUntil     → DateTimeOffset? LockedUntil
DateTime CreatedAt        → DateTimeOffset CreatedAt
DateTime UpdatedAt        → DateTimeOffset UpdatedAt
DateTime? LastLoginAt     → DateTimeOffset? LastLoginAt
```

**Factory methods to update**:
- `Create()` (line 46-48): Change `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `CreateFromOAuth()` (line 64-65): Change `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

**Methods to update**:
- `MarkEmailAsVerified()` (line 74): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `RecordSuccessfulLogin()` (line 81-82): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `RecordFailedLogin()` (lines 88, 93): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `IsLockedOut()` (line 99): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `Suspend()` (line 105): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `Activate()` (line 111): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `ChangePassword()` (line 119): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `PromoteToAdmin()` (line 125): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

#### File: `apps/backend/src/Codewrinkles.Domain/Identity/Profile.cs`

**Properties to change** (lines 21-22):
```
DateTime CreatedAt  → DateTimeOffset CreatedAt
DateTime UpdatedAt  → DateTimeOffset UpdatedAt
```

**Factory method to update**:
- `Create()` (lines 47-48): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

**Methods to update**:
- `UpdateProfile()` (line 62): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `UpdateProfileDetails()` (line 80): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `UpdateAvatarUrl()` (line 86): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `CompleteOnboarding()` (line 92): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

#### File: `apps/backend/src/Codewrinkles.Domain/Identity/RefreshToken.cs`

**Properties to change** (lines 20-21, 24):
```
DateTime CreatedAt    → DateTimeOffset CreatedAt
DateTime ExpiresAt    → DateTimeOffset ExpiresAt
DateTime? RevokedAt   → DateTimeOffset? RevokedAt
```

**Factory method signature change** (line 41):
```csharp
// FROM:
public static RefreshToken Create(string tokenHash, Guid identityId, DateTime expiresAt)
// TO:
public static RefreshToken Create(string tokenHash, Guid identityId, DateTimeOffset expiresAt)
```

**Factory method body** (lines 45, 55-56):
- Line 45: `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- Line 55: `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

**Methods to update**:
- `IsExpired()` (line 75): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `Revoke()` (line 95): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

#### File: `apps/backend/src/Codewrinkles.Domain/Identity/ExternalLogin.cs`

**Properties to change** (lines 14-16):
```
DateTime? TokenExpiresAt  → DateTimeOffset? TokenExpiresAt
DateTime CreatedAt        → DateTimeOffset CreatedAt
DateTime UpdatedAt        → DateTimeOffset UpdatedAt
```

**Factory method signature** (line 35):
```csharp
// FROM:
DateTime? tokenExpiresAt = null
// TO:
DateTimeOffset? tokenExpiresAt = null
```

**Factory method body** (lines 51-52):
- `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

**Methods to update**:
- `UpdateTokens()` signature (line 56): `DateTime expiresAt` → `DateTimeOffset expiresAt`
- `UpdateTokens()` body (line 62): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

#### File: `apps/backend/src/Codewrinkles.Domain/Identity/JwtTokenGenerator.cs`

**Line 54**:
```csharp
// FROM:
expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes)
// TO:
expires: DateTimeOffset.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes).UtcDateTime
```

**Note**: `JwtSecurityToken` requires `DateTime`, so we extract `.UtcDateTime` from `DateTimeOffset`.

---

### 1.2 Pulse Module

#### File: `apps/backend/src/Codewrinkles.Domain/Pulse/Pulse.cs`

**Properties to change** (lines 24-25):
```
DateTime CreatedAt    → DateTimeOffset CreatedAt
DateTime? UpdatedAt   → DateTimeOffset? UpdatedAt
```

**Factory methods to update**:
- `Create()` (line 61): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `CreateRepulse()` (line 90): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `CreateReply()` (line 120): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

**Methods to update**:
- `MarkAsDeleted()` (line 130): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

#### File: `apps/backend/src/Codewrinkles.Domain/Pulse/PulseLike.cs`

**Property to change** (line 16):
```
DateTime CreatedAt → DateTimeOffset CreatedAt
```

**Factory method** (line 36):
- `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

#### File: `apps/backend/src/Codewrinkles.Domain/Pulse/PulseBookmark.cs`

**Property to change** (line 17):
```
DateTime CreatedAt → DateTimeOffset CreatedAt
```

**Factory method** (line 34):
- `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

#### File: `apps/backend/src/Codewrinkles.Domain/Pulse/PulseMention.cs`

**Property to change** (line 13):
```
DateTime CreatedAt → DateTimeOffset CreatedAt
```

**Factory method** (line 35):
- `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

#### File: `apps/backend/src/Codewrinkles.Domain/Pulse/PulseLinkPreview.cs`

**Property to change** (line 13):
```
DateTime CreatedAt → DateTimeOffset CreatedAt
```

**Factory method** (line 46):
- `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

#### File: `apps/backend/src/Codewrinkles.Domain/Pulse/Hashtag.cs`

**Properties to change** (lines 10-11):
```
DateTime LastUsedAt → DateTimeOffset LastUsedAt
DateTime CreatedAt  → DateTimeOffset CreatedAt
```

**Factory method** (lines 35-36):
- `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

**Methods to update**:
- `IncrementUsage()` (line 44): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

#### File: `apps/backend/src/Codewrinkles.Domain/Pulse/PulseHashtag.cs`

**Property to change** (line 9):
```
DateTime CreatedAt → DateTimeOffset CreatedAt
```

**Factory method** (line 29):
- `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

### 1.3 Social Module

#### File: `apps/backend/src/Codewrinkles.Domain/Social/Follow.cs`

**Property to change** (line 21):
```
DateTime CreatedAt → DateTimeOffset CreatedAt
```

**Factory method** (line 42):
- `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

### 1.4 Notification Module

#### File: `apps/backend/src/Codewrinkles.Domain/Notification/Notification.cs`

**Properties to change** (lines 21-22):
```
DateTime CreatedAt  → DateTimeOffset CreatedAt
DateTime? ReadAt    → DateTimeOffset? ReadAt
```

**Factory methods** (all 5):
- `CreateLikeNotification()` (line 43): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `CreateReplyNotification()` (line 61): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `CreateRepulseNotification()` (line 79): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `CreateMentionNotification()` (line 97): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `CreateFollowNotification()` (line 114): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

**Methods to update**:
- `MarkAsRead()` (line 125): `DateTime.UtcNow` → `DateTimeOffset.UtcNow`

---

## Phase 2: EF Core Configuration Changes

### 2.1 Identity Module Configurations

#### File: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Identity/IdentityConfiguration.cs`

**Lines 50-59** - Change `GETUTCDATE()` to `SYSDATETIMEOFFSET()`:
```csharp
// FROM:
builder.Property(i => i.CreatedAt)
    .IsRequired()
    .HasDefaultValueSql("GETUTCDATE()");

builder.Property(i => i.UpdatedAt)
    .IsRequired()
    .HasDefaultValueSql("GETUTCDATE()");

// TO:
builder.Property(i => i.CreatedAt)
    .IsRequired()
    .HasDefaultValueSql("SYSDATETIMEOFFSET()");

builder.Property(i => i.UpdatedAt)
    .IsRequired()
    .HasDefaultValueSql("SYSDATETIMEOFFSET()");
```

**Note**: `LockedUntil` and `LastLoginAt` don't have defaults - no changes needed.

---

#### File: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Identity/ProfileConfiguration.cs`

**Lines 50-56**:
```csharp
// FROM:
builder.Property(p => p.CreatedAt)
    .IsRequired()
    .HasDefaultValueSql("GETUTCDATE()");

builder.Property(p => p.UpdatedAt)
    .IsRequired()
    .HasDefaultValueSql("GETUTCDATE()");

// TO:
builder.Property(p => p.CreatedAt)
    .IsRequired()
    .HasDefaultValueSql("SYSDATETIMEOFFSET()");

builder.Property(p => p.UpdatedAt)
    .IsRequired()
    .HasDefaultValueSql("SYSDATETIMEOFFSET()");
```

---

#### File: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`

**No changes needed** - `CreatedAt` and `ExpiresAt` don't have database defaults (set in factory method).

---

#### File: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Identity/ExternalLoginConfiguration.cs`

**Needs verification** - Check if any datetime columns have defaults. If so, change `GETUTCDATE()` → `SYSDATETIMEOFFSET()`.

---

### 2.2 Pulse Module Configurations

#### File: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Pulse/PulseConfiguration.cs`

**Lines 30-35**:
```csharp
// FROM:
builder.Property(p => p.CreatedAt)
    .IsRequired()
    .HasDefaultValueSql("GETUTCDATE()");

// TO:
builder.Property(p => p.CreatedAt)
    .IsRequired()
    .HasDefaultValueSql("SYSDATETIMEOFFSET()");
```

**Note**: `UpdatedAt` is nullable without a default - no changes needed.

---

#### Files with no default value changes needed:
- `PulseLikeConfiguration.cs` - No datetime defaults
- `PulseBookmarkConfiguration.cs` - No datetime defaults
- `PulseMentionConfiguration.cs` - No datetime defaults
- `PulseLinkPreviewConfiguration.cs` - No datetime defaults
- `HashtagConfiguration.cs` - Verify if any defaults exist
- `PulseHashtagConfiguration.cs` - No datetime defaults

---

### 2.3 Social Module Configurations

#### File: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Social/FollowConfiguration.cs`

**No changes needed** - `CreatedAt` has no database default (set in factory method).

---

### 2.4 Notification Module Configurations

#### File: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Configurations/Notification/NotificationConfiguration.cs`

**No changes needed** - `CreatedAt` has no database default (set in factory method).

---

## Phase 3: Application Layer DTO Changes

### 3.1 Pulse DTOs

#### File: `apps/backend/src/Codewrinkles.Application/Pulse/PulseDtos.cs`

**Line 8** (PulseDto):
```csharp
DateTime CreatedAt → DateTimeOffset CreatedAt
```

**Line 55** (RepulsedPulseDto):
```csharp
DateTime CreatedAt → DateTimeOffset CreatedAt
```

---

#### File: `apps/backend/src/Codewrinkles.Application/Pulse/CreatePulse.cs`

**Line 18** (CreatePulseResult):
```csharp
DateTime CreatedAt → DateTimeOffset CreatedAt
```

---

#### File: `apps/backend/src/Codewrinkles.Application/Pulse/GetTrendingHashtags.cs`

**Line 15** (HashtagDto):
```csharp
DateTime LastUsedAt → DateTimeOffset LastUsedAt
```

---

### 3.2 Social DTOs

#### File: `apps/backend/src/Codewrinkles.Application/Social/SocialDtos.cs`

**Line 13** (FollowerDto):
```csharp
DateTime FollowedAt → DateTimeOffset FollowedAt
```

**Line 23** (FollowingDto):
```csharp
DateTime FollowedAt → DateTimeOffset FollowedAt
```

---

### 3.3 Notification DTOs

#### File: `apps/backend/src/Codewrinkles.Application/Notification/GetNotifications.cs`

**Line 25** (NotificationDto):
```csharp
DateTime CreatedAt → DateTimeOffset CreatedAt
```

---

### 3.4 Exception Classes

#### File: `apps/backend/src/Codewrinkles.Application/Users/LoginUser.cs`

**Line 149** (AccountLockedException):
```csharp
// FROM:
public DateTime LockedUntil { get; }
public AccountLockedException(DateTime lockedUntil)

// TO:
public DateTimeOffset LockedUntil { get; }
public AccountLockedException(DateTimeOffset lockedUntil)
```

---

## Phase 4: Application Layer Handler Changes

### 4.1 Cursor-Based Pagination

The following files use `DateTime` for cursor encoding/decoding. All need to change to `DateTimeOffset`:

#### File: `apps/backend/src/Codewrinkles.Application/Pulse/GetFeed.cs`

**Lines 40, 184-213**:
```csharp
// Change all occurrences:
DateTime? beforeCreatedAt     → DateTimeOffset? beforeCreatedAt
DateTime CreatedAt            → DateTimeOffset CreatedAt
CursorData(DateTime, Guid)    → CursorData(DateTimeOffset, Guid)
```

---

#### File: `apps/backend/src/Codewrinkles.Application/Pulse/GetPulsesByAuthor.cs`

**Lines 30, 159-188**:
```csharp
// Same pattern as GetFeed.cs
DateTime? beforeCreatedAt     → DateTimeOffset? beforeCreatedAt
DateTime CreatedAt            → DateTimeOffset CreatedAt
CursorData(DateTime, Guid)    → CursorData(DateTimeOffset, Guid)
```

---

#### File: `apps/backend/src/Codewrinkles.Application/Pulse/GetPulsesByHashtag.cs`

**Lines 30, 138-167**:
```csharp
// Same pattern as GetFeed.cs
DateTime? beforeCreatedAt     → DateTimeOffset? beforeCreatedAt
DateTime CreatedAt            → DateTimeOffset CreatedAt
CursorData(DateTime, Guid)    → CursorData(DateTimeOffset, Guid)
```

---

#### File: `apps/backend/src/Codewrinkles.Application/Pulse/GetBookmarkedPulses.cs`

**Lines 35, 166-195**:
```csharp
// Same pattern as GetFeed.cs
DateTime? beforeCreatedAt     → DateTimeOffset? beforeCreatedAt
DateTime CreatedAt            → DateTimeOffset CreatedAt
CursorData(DateTime, Guid)    → CursorData(DateTimeOffset, Guid)
```

---

#### File: `apps/backend/src/Codewrinkles.Application/Pulse/GetThread.cs`

**Line 13**:
```csharp
DateTime? BeforeCreatedAt → DateTimeOffset? BeforeCreatedAt
```

---

### 4.2 Token Expiration

#### File: `apps/backend/src/Codewrinkles.Application/Users/LoginUser.cs`

**Line 87**:
```csharp
// FROM:
var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtTokenGenerator.RefreshTokenExpiryDays);

// TO:
var refreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(_jwtTokenGenerator.RefreshTokenExpiryDays);
```

---

#### File: `apps/backend/src/Codewrinkles.Application/Users/RefreshAccessToken.cs`

Search for any `DateTime.UtcNow` usage and replace with `DateTimeOffset.UtcNow`.

---

#### File: `apps/backend/src/Codewrinkles.Application/Users/RegisterUser.cs`

Search for any `DateTime.UtcNow` usage and replace with `DateTimeOffset.UtcNow`.

---

#### File: `apps/backend/src/Codewrinkles.Application/Users/CompleteOAuthCallback.cs`

Search for any `DateTime.UtcNow` usage and replace with `DateTimeOffset.UtcNow`.

---

## Phase 5: Repository Interface Changes

### 5.1 Pulse Repository

#### File: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IPulseRepository.cs`

**All method signatures with `DateTime?` parameters**:

```csharp
// Lines 45-47:
Task<IReadOnlyList<PulseEntity>> GetFeedAsync(
    Guid? currentUserId,
    int limit,
    DateTimeOffset? beforeCreatedAt,  // Changed from DateTime?
    Guid? beforeId,
    CancellationToken cancellationToken);

// Lines 55-60:
Task<FeedData> GetFeedWithMetadataAsync(
    Guid? currentUserId,
    int limit,
    DateTimeOffset? beforeCreatedAt,  // Changed from DateTime?
    Guid? beforeId,
    CancellationToken cancellationToken);

// Lines 66-71:
Task<IReadOnlyList<PulseEntity>> GetByAuthorIdAsync(
    Guid authorId,
    int limit,
    DateTimeOffset? beforeCreatedAt,  // Changed from DateTime?
    Guid? beforeId,
    CancellationToken cancellationToken);

// Lines 79-85:
Task<FeedData> GetPulsesByAuthorWithMetadataAsync(
    Guid authorId,
    Guid? currentUserId,
    int limit,
    DateTimeOffset? beforeCreatedAt,  // Changed from DateTime?
    Guid? beforeId,
    CancellationToken cancellationToken);

// Lines 166-171:
Task<IReadOnlyList<PulseEntity>> GetRepliesByThreadRootIdAsync(
    Guid threadRootId,
    int limit,
    DateTimeOffset? beforeCreatedAt,  // Changed from DateTime?
    Guid? beforeId,
    CancellationToken cancellationToken);
```

---

### 5.2 Other Repository Interfaces

Check and update these files for any `DateTime` parameters:

- `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IIdentityRepository.cs`
- `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IHashtagRepository.cs`
- `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IBookmarkRepository.cs`
- `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IFollowRepository.cs`

---

## Phase 6: Repository Implementation Changes

### 6.1 Pulse Repository

#### File: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/PulseRepository.cs`

Update all method implementations to use `DateTimeOffset?` parameters matching the interface changes.

---

### 6.2 RefreshToken Repository

#### File: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs`

**Line 44** (and similar):
```csharp
// FROM:
.Where(rt => rt.ExpiresAt < DateTime.UtcNow)

// TO:
.Where(rt => rt.ExpiresAt < DateTimeOffset.UtcNow)
```

---

## Phase 7: Database Migration

### 7.1 Create Migration

After all code changes are complete, create a new migration:

```bash
cd apps/backend/src/Codewrinkles.API
dotnet ef migrations add MigrateDateTimeToDateTimeOffset --project ../Codewrinkles.Infrastructure
```

### 7.2 Expected Migration Content

The migration should:
1. Alter all `datetime2` columns to `datetimeoffset`
2. Change all `GETUTCDATE()` defaults to `SYSDATETIMEOFFSET()`

**Example of what EF Core should generate**:
```csharp
// For each column:
migrationBuilder.AlterColumn<DateTimeOffset>(
    name: "CreatedAt",
    table: "Pulses",
    schema: "pulse",
    type: "datetimeoffset",
    nullable: false,
    defaultValueSql: "SYSDATETIMEOFFSET()",
    oldClrType: typeof(DateTime),
    oldType: "datetime2",
    oldDefaultValueSql: "GETUTCDATE()");
```

### 7.3 Data Migration Consideration

**Important**: Existing `datetime2` data will be converted to `datetimeoffset` with offset `+00:00` (UTC). This is correct because all existing data was stored as UTC.

SQL Server handles this conversion automatically:
```sql
-- Implicit conversion:
-- datetime2: 2025-12-07 10:30:00.0000000
-- → datetimeoffset: 2025-12-07 10:30:00.0000000 +00:00
```

### 7.4 Apply Migration

**Local Development**:
```bash
dotnet ef database update --project ../Codewrinkles.Infrastructure
```

**Production**: Apply via your deployment pipeline.

---

## Phase 8: Frontend Verification

### 8.1 No Code Changes Required

The frontend types use `string` for all timestamps:
```typescript
// apps/frontend/src/types.ts
createdAt: string;  // Already a string - no change needed
```

### 8.2 Verify Correct Parsing

After backend changes, the API will return:
```json
{
  "createdAt": "2025-12-07T10:30:00+00:00"
}
```

JavaScript `new Date("2025-12-07T10:30:00+00:00")` correctly parses this as UTC.

### 8.3 Test formatTimeAgo

#### File: `apps/frontend/src/utils/timeUtils.ts`

The existing code should work correctly with the new format:
```typescript
const date = new Date(isoDate);  // Now correctly parsed as UTC
const now = new Date();          // Local time
// Difference calculation will be correct
```

---

## Phase 9: Testing Checklist

### 9.1 Unit Tests

- [ ] All domain entity factory methods create correct `DateTimeOffset.UtcNow` values
- [ ] All domain entity methods update timestamps correctly
- [ ] Cursor encoding/decoding works with `DateTimeOffset`

### 9.2 Integration Tests

- [ ] API responses include timezone offset in timestamps
- [ ] Pagination cursors decode correctly
- [ ] Token expiration checks work correctly
- [ ] Account lockout timing works correctly

### 9.3 E2E Tests

- [ ] Create a pulse → displays "just now" or "0s" regardless of user timezone
- [ ] Notifications display correct relative time
- [ ] Feed pagination works correctly

### 9.4 Cross-Timezone Verification

Test from multiple timezones (or simulate by changing system timezone):
- [ ] UTC+0 (UK)
- [ ] UTC+2 (Romania)
- [ ] UTC-5 (US East)
- [ ] UTC+9 (Japan)

---

## Phase 10: Rollback Plan

If issues arise after deployment:

### 10.1 Code Rollback
Revert all code changes via git.

### 10.2 Database Rollback
```bash
dotnet ef database update PreviousMigrationName --project ../Codewrinkles.Infrastructure
```

### 10.3 Data Consideration
Data in `datetimeoffset` columns can be safely converted back to `datetime2` - the offset information will be stripped but values remain correct (they're all UTC anyway).

---

## Implementation Order

1. **Phase 1**: Domain entities (all 13 files)
2. **Phase 2**: EF Core configurations (15 files)
3. **Phase 3**: Application DTOs (6 files)
4. **Phase 4**: Application handlers (~22 files)
5. **Phase 5**: Repository interfaces (4 files)
6. **Phase 6**: Repository implementations (matching files)
7. **Phase 11**: Fix FollowedAt bug (4 files) - do this together with Phase 5/6
8. **Build & Fix**: Compile and fix any missed references
9. **Phase 7**: Create and apply database migration
10. **Phase 8**: Frontend verification (no changes expected)
11. **Phase 9**: Testing
12. **Phase 12**: Update CLAUDE.md with DateTime guidelines (1 file)

---

## Files Summary

### Domain Layer (13 files)
1. `Codewrinkles.Domain/Identity/Identity.cs`
2. `Codewrinkles.Domain/Identity/Profile.cs`
3. `Codewrinkles.Domain/Identity/RefreshToken.cs`
4. `Codewrinkles.Domain/Identity/ExternalLogin.cs`
5. `Codewrinkles.Domain/Identity/JwtTokenGenerator.cs`
6. `Codewrinkles.Domain/Pulse/Pulse.cs`
7. `Codewrinkles.Domain/Pulse/PulseLike.cs`
8. `Codewrinkles.Domain/Pulse/PulseBookmark.cs`
9. `Codewrinkles.Domain/Pulse/PulseMention.cs`
10. `Codewrinkles.Domain/Pulse/PulseLinkPreview.cs`
11. `Codewrinkles.Domain/Pulse/Hashtag.cs`
12. `Codewrinkles.Domain/Pulse/PulseHashtag.cs`
13. `Codewrinkles.Domain/Social/Follow.cs`
14. `Codewrinkles.Domain/Notification/Notification.cs`

### Infrastructure Layer - Configurations (15 files)
1. `Configurations/Identity/IdentityConfiguration.cs`
2. `Configurations/Identity/ProfileConfiguration.cs`
3. `Configurations/Identity/ExternalLoginConfiguration.cs`
4. `Configurations/RefreshTokenConfiguration.cs`
5. `Configurations/Pulse/PulseConfiguration.cs`
6. `Configurations/Pulse/PulseLikeConfiguration.cs`
7. `Configurations/Pulse/PulseBookmarkConfiguration.cs`
8. `Configurations/Pulse/PulseMentionConfiguration.cs`
9. `Configurations/Pulse/PulseLinkPreviewConfiguration.cs`
10. `Configurations/Pulse/HashtagConfiguration.cs`
11. `Configurations/Pulse/PulseHashtagConfiguration.cs`
12. `Configurations/Pulse/PulseEngagementConfiguration.cs`
13. `Configurations/Pulse/PulseImageConfiguration.cs`
14. `Configurations/Social/FollowConfiguration.cs`
15. `Configurations/Notification/NotificationConfiguration.cs`

### Application Layer - DTOs & Handlers (~28 files)
- See Phase 3 and Phase 4 for complete list

### Phase 11 - FollowedAt Bug Fix (4 files)
1. `Application/Common/Interfaces/IFollowRepository.cs` - Add `ProfileWithFollowDate`, update signatures
2. `Infrastructure/Persistence/Repositories/FollowRepository.cs` - Update implementations
3. `Application/Social/GetFollowers.cs` - Fix cursor and DTO mapping
4. `Application/Social/GetFollowing.cs` - Fix cursor and DTO mapping

### Phase 12 - Documentation (1 file)
1. `CLAUDE.md` - Add "DateTime and Timestamp Handling" guidelines section

### Frontend (0 changes expected)
- `timeUtils.ts` - verify only

---

## Phase 11: Fix FollowedAt Bug

### Problem Description

The `GetFollowers` and `GetFollowing` handlers currently return `DateTime.UtcNow` instead of the actual `Follow.CreatedAt` timestamp. This means the "Followed at" date is always wrong - it shows the current time instead of when the user actually followed.

**Current buggy code**:
```csharp
// GetFollowers.cs:84
FollowedAt: DateTime.UtcNow, // TODO: This should come from Follow.CreatedAt

// GetFollowing.cs:82
FollowedAt: DateTime.UtcNow, // TODO: This should come from Follow.CreatedAt
```

**Additional issue**: The cursor pagination also uses `DateTime.UtcNow` instead of the actual `Follow.CreatedAt`:
```csharp
// GetFollowers.cs:63
nextCursor = EncodeCursor(DateTime.UtcNow, lastFollower.Id);

// GetFollowing.cs:61
nextCursor = EncodeCursor(DateTime.UtcNow, lastFollowing.Id);
```

### Root Cause

The repository methods return `IReadOnlyList<Profile>` but we need `Follow.CreatedAt` as well. The repository doesn't include the follow relationship data.

### Solution

Create a new record type that includes both Profile and Follow.CreatedAt, then update the repository interface and implementation.

---

### 11.1 Create New DTO Type

#### File: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IFollowRepository.cs`

**Add new record at top of file** (after the using statements):
```csharp
/// <summary>
/// Represents a profile with its associated follow timestamp.
/// Used by GetFollowersAsync and GetFollowingAsync to return the actual follow date.
/// </summary>
public sealed record ProfileWithFollowDate(
    Profile Profile,
    DateTimeOffset FollowedAt
);
```

---

### 11.2 Update Repository Interface

#### File: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/IFollowRepository.cs`

**Change method signatures** (lines 14-26):

```csharp
// FROM:
Task<IReadOnlyList<Profile>> GetFollowersAsync(
    Guid profileId,
    int limit,
    DateTime? beforeCreatedAt,
    Guid? beforeId,
    CancellationToken cancellationToken);

Task<IReadOnlyList<Profile>> GetFollowingAsync(
    Guid profileId,
    int limit,
    DateTime? beforeCreatedAt,
    Guid? beforeId,
    CancellationToken cancellationToken);

// TO:
Task<IReadOnlyList<ProfileWithFollowDate>> GetFollowersAsync(
    Guid profileId,
    int limit,
    DateTimeOffset? beforeCreatedAt,
    Guid? beforeId,
    CancellationToken cancellationToken);

Task<IReadOnlyList<ProfileWithFollowDate>> GetFollowingAsync(
    Guid profileId,
    int limit,
    DateTimeOffset? beforeCreatedAt,
    Guid? beforeId,
    CancellationToken cancellationToken);
```

---

### 11.3 Update Repository Implementation

#### File: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/FollowRepository.cs`

**Update GetFollowersAsync**:
```csharp
public async Task<IReadOnlyList<ProfileWithFollowDate>> GetFollowersAsync(
    Guid profileId,
    int limit,
    DateTimeOffset? beforeCreatedAt,
    Guid? beforeId,
    CancellationToken cancellationToken)
{
    var query = _follows
        .Where(f => f.FollowingId == profileId)
        .Include(f => f.Follower);

    // Apply cursor-based pagination
    if (beforeCreatedAt.HasValue && beforeId.HasValue)
    {
        query = query.Where(f =>
            f.CreatedAt < beforeCreatedAt.Value ||
            (f.CreatedAt == beforeCreatedAt.Value && f.FollowerId.CompareTo(beforeId.Value) < 0));
    }

    var follows = await query
        .OrderByDescending(f => f.CreatedAt)
        .ThenByDescending(f => f.FollowerId)
        .Take(limit)
        .ToListAsync(cancellationToken);

    return follows
        .Select(f => new ProfileWithFollowDate(f.Follower, f.CreatedAt))
        .ToList();
}
```

**Update GetFollowingAsync** (similar pattern):
```csharp
public async Task<IReadOnlyList<ProfileWithFollowDate>> GetFollowingAsync(
    Guid profileId,
    int limit,
    DateTimeOffset? beforeCreatedAt,
    Guid? beforeId,
    CancellationToken cancellationToken)
{
    var query = _follows
        .Where(f => f.FollowerId == profileId)
        .Include(f => f.Following);

    // Apply cursor-based pagination
    if (beforeCreatedAt.HasValue && beforeId.HasValue)
    {
        query = query.Where(f =>
            f.CreatedAt < beforeCreatedAt.Value ||
            (f.CreatedAt == beforeCreatedAt.Value && f.FollowingId.CompareTo(beforeId.Value) < 0));
    }

    var follows = await query
        .OrderByDescending(f => f.CreatedAt)
        .ThenByDescending(f => f.FollowingId)
        .Take(limit)
        .ToListAsync(cancellationToken);

    return follows
        .Select(f => new ProfileWithFollowDate(f.Following, f.CreatedAt))
        .ToList();
}
```

---

### 11.4 Update GetFollowers Handler

#### File: `apps/backend/src/Codewrinkles.Application/Social/GetFollowers.cs`

**Update cursor types** (lines 29, 96-125):
```csharp
// Line 29:
DateTimeOffset? beforeCreatedAt = null;  // Changed from DateTime?

// Lines 96, 104, 125:
// Change all DateTime references to DateTimeOffset in cursor methods
private static string EncodeCursor(DateTimeOffset createdAt, Guid id)
private static (DateTimeOffset CreatedAt, Guid Id) DecodeCursor(string cursor)
private sealed record CursorData(DateTimeOffset CreatedAt, Guid Id);
```

**Update the handler body** - fix next cursor (line 58-64):
```csharp
// FROM:
if (hasMore)
{
    var lastFollower = followersToReturn.Last();
    nextCursor = EncodeCursor(DateTime.UtcNow, lastFollower.Id);
}

// TO:
if (hasMore)
{
    var lastFollowerData = followersToReturn.Last();
    nextCursor = EncodeCursor(lastFollowerData.FollowedAt, lastFollowerData.Profile.Id);
}
```

**Update the DTO mapping** (lines 77-86):
```csharp
// FROM:
var followerDtos = followersToReturn.Select(p => new FollowerDto(
    ProfileId: p.Id,
    Name: p.Name,
    Handle: p.Handle ?? string.Empty,
    AvatarUrl: p.AvatarUrl,
    Bio: p.Bio,
    FollowedAt: DateTime.UtcNow, // TODO: This should come from Follow.CreatedAt
    IsFollowing: followingProfileIds.Contains(p.Id)
)).ToList();

// TO:
var followerDtos = followersToReturn.Select(data => new FollowerDto(
    ProfileId: data.Profile.Id,
    Name: data.Profile.Name,
    Handle: data.Profile.Handle ?? string.Empty,
    AvatarUrl: data.Profile.AvatarUrl,
    Bio: data.Profile.Bio,
    FollowedAt: data.FollowedAt,  // Now using actual Follow.CreatedAt!
    IsFollowing: followingProfileIds.Contains(data.Profile.Id)
)).ToList();
```

**Update follower ID extraction for batch query** (line 70):
```csharp
// FROM:
var followerIds = followersToReturn.Select(p => p.Id).ToList();

// TO:
var followerIds = followersToReturn.Select(data => data.Profile.Id).ToList();
```

---

### 11.5 Update GetFollowing Handler

#### File: `apps/backend/src/Codewrinkles.Application/Social/GetFollowing.cs`

Apply the same pattern as GetFollowers:

**Update cursor types** (lines 29, 94-123):
```csharp
// Line 29:
DateTimeOffset? beforeCreatedAt = null;  // Changed from DateTime?

// Cursor methods - change to DateTimeOffset
private static string EncodeCursor(DateTimeOffset createdAt, Guid id)
private static (DateTimeOffset CreatedAt, Guid Id) DecodeCursor(string cursor)
private sealed record CursorData(DateTimeOffset CreatedAt, Guid Id);
```

**Update next cursor** (lines 58-62):
```csharp
// FROM:
if (hasMore)
{
    var lastFollowing = followingToReturn.Last();
    nextCursor = EncodeCursor(DateTime.UtcNow, lastFollowing.Id);
}

// TO:
if (hasMore)
{
    var lastFollowingData = followingToReturn.Last();
    nextCursor = EncodeCursor(lastFollowingData.FollowedAt, lastFollowingData.Profile.Id);
}
```

**Update DTO mapping** (lines 76-84):
```csharp
// FROM:
var followingDtos = followingToReturn.Select(p => new FollowingDto(
    ProfileId: p.Id,
    Name: p.Name,
    Handle: p.Handle ?? string.Empty,
    AvatarUrl: p.AvatarUrl,
    Bio: p.Bio,
    FollowedAt: DateTime.UtcNow, // TODO: This should come from Follow.CreatedAt
    IsFollowing: followingProfileIds.Contains(p.Id)
)).ToList();

// TO:
var followingDtos = followingToReturn.Select(data => new FollowingDto(
    ProfileId: data.Profile.Id,
    Name: data.Profile.Name,
    Handle: data.Profile.Handle ?? string.Empty,
    AvatarUrl: data.Profile.AvatarUrl,
    Bio: data.Profile.Bio,
    FollowedAt: data.FollowedAt,  // Now using actual Follow.CreatedAt!
    IsFollowing: followingProfileIds.Contains(data.Profile.Id)
)).ToList();
```

**Update profile ID extraction** (line 68):
```csharp
// FROM:
var profileIds = followingToReturn.Select(p => p.Id).ToList();

// TO:
var profileIds = followingToReturn.Select(data => data.Profile.Id).ToList();
```

---

### 11.6 Files Summary for Phase 11

| File | Changes |
|------|---------|
| `IFollowRepository.cs` | Add `ProfileWithFollowDate` record, change method return types and parameters |
| `FollowRepository.cs` | Update implementations to return `ProfileWithFollowDate` |
| `GetFollowers.cs` | Fix cursor pagination, fix DTO mapping to use actual `FollowedAt` |
| `GetFollowing.cs` | Fix cursor pagination, fix DTO mapping to use actual `FollowedAt` |

---

## Phase 12: Update CLAUDE.md with DateTime Guidelines

### Purpose

Document the DateTime handling conventions so future development sessions maintain consistency.

### File to Update

`CLAUDE.md` (repository root)

### Section to Add

Add a new section after the "GUID Primary Keys" section titled **"DateTime and Timestamp Handling"**.

### Content to Add

```markdown
---

### 🚨 CRITICAL: DateTime and Timestamp Handling

**✅ ALWAYS USE `DateTimeOffset` - NEVER USE `DateTime`**

This is a **non-negotiable standard** for all timestamp handling in the codebase.

**Why DateTimeOffset:**
- ✅ **Stores timezone offset** - SQL Server `datetimeoffset` preserves UTC offset
- ✅ **Correct JSON serialization** - System.Text.Json outputs `"2025-12-07T10:30:00+00:00"`
- ✅ **JavaScript compatibility** - Browsers correctly parse timezone-aware ISO strings
- ❌ `DateTime` with `datetime2` loses timezone info on database round-trip
- ❌ `DateTime` with `Kind = Unspecified` serializes without 'Z' suffix, breaking frontend

**The Correct Pattern:**

```csharp
// ✅ CORRECT - Always use DateTimeOffset.UtcNow
public DateTimeOffset CreatedAt { get; private set; }
public DateTimeOffset UpdatedAt { get; private set; }
public DateTimeOffset? DeletedAt { get; private set; }

public static MyEntity Create()
{
    return new MyEntity
    {
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}

// ❌ WRONG - Never use DateTime
public DateTime CreatedAt { get; private set; }  // DON'T DO THIS
CreatedAt = DateTime.UtcNow  // DON'T DO THIS
```

**EF Core Configuration:**

```csharp
// ✅ CORRECT - Use SYSDATETIMEOFFSET() for defaults
builder.Property(e => e.CreatedAt)
    .IsRequired()
    .HasDefaultValueSql("SYSDATETIMEOFFSET()");

// ❌ WRONG - GETUTCDATE() returns datetime, not datetimeoffset
builder.Property(e => e.CreatedAt)
    .HasDefaultValueSql("GETUTCDATE()");  // DON'T DO THIS
```

**DTOs:**

```csharp
// ✅ CORRECT - DTOs use DateTimeOffset
public sealed record PulseDto(
    Guid Id,
    string Content,
    DateTimeOffset CreatedAt  // Will serialize as "2025-12-07T10:30:00+00:00"
);
```

**Comparisons:**

```csharp
// ✅ CORRECT - Compare with DateTimeOffset.UtcNow
public bool IsExpired() => DateTimeOffset.UtcNow >= ExpiresAt;

public bool IsLockedOut() => LockedUntil.HasValue && LockedUntil.Value > DateTimeOffset.UtcNow;
```

**JWT Token Expiration (special case):**

```csharp
// JwtSecurityToken requires DateTime, so extract .UtcDateTime
var token = new JwtSecurityToken(
    expires: DateTimeOffset.UtcNow.AddMinutes(expiryMinutes).UtcDateTime,
    // ...
);
```

**Rules:**
1. ✅ **ALWAYS** use `DateTimeOffset` for all timestamp properties
2. ✅ **ALWAYS** use `DateTimeOffset.UtcNow` (not `.Now`)
3. ✅ **ALWAYS** use `SYSDATETIMEOFFSET()` for SQL Server defaults
4. ✅ SQL Server will use `datetimeoffset` column type automatically
5. ❌ **NEVER** use `DateTime` for new code
6. ❌ **NEVER** use `DateTime.Now` (local time)
7. ❌ **NEVER** use `GETUTCDATE()` for defaults (returns `datetime`, not `datetimeoffset`)

**Why This Matters - The Bug We Fixed:**

Using `DateTime` caused timestamps to display incorrectly across timezones:
1. Backend stored UTC time in `datetime2` (no timezone info)
2. EF Core read it back with `Kind = Unspecified`
3. JSON serialized as `"2025-12-07T10:30:00"` (no Z suffix)
4. JavaScript parsed as local time instead of UTC
5. Users in UTC+2 saw "2h ago" for posts created "just now"

`DateTimeOffset` fixes this by preserving the offset through the entire stack.

---
```

### Where to Insert

Insert after the "GUID Primary Keys - Sequential Generation" section (around line 200 in CLAUDE.md).

### Files Summary for Phase 12

| File | Changes |
|------|---------|
| `CLAUDE.md` | Add "DateTime and Timestamp Handling" section |

---

## Notes

- All timestamps should use `DateTimeOffset.UtcNow` (not `DateTimeOffset.Now`)
- The offset will always be `+00:00` since we use UTC
- SQL Server `datetimeoffset` stores the offset, ensuring consistent serialization
- System.Text.Json will serialize as `"2025-12-07T10:30:00+00:00"`
- JavaScript correctly handles ISO 8601 strings with offset

---

**Last Updated**: 2025-12-07
