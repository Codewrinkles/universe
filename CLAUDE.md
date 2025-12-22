# Claude Session Guide - Codewrinkles Universe

> **🚨 CRITICAL: NEVER commit or push without explicit user permission. Each push triggers a production deployment and causes downtime. Always wait for the user to say "commit", "push", or explicitly ask for it.**

> **Purpose**: Quick reference for Claude Code sessions working on this codebase. Read this at the start of each session along with `core.md`.

---

## Tech Stack

### Frontend (`apps/frontend/`)
- **Build Tool**: Vite 6.4.1
- **Framework**: React 18.3.1
- **Language**: TypeScript 5.7.3 (strict mode)
- **Styling**: TailwindCSS 3.4.17
- **Routing**: React Router DOM 7.1.1
- **Node**: 20+

### Backend (`apps/backend/`)
- **Framework**: .NET 10, ASP.NET Core Minimal APIs
- **Language**: C# 13
- **Database**: SQL Server (latest)
- **ORM**: Entity Framework Core 10
- **Architecture**: Clean Architecture (layered monolith)
- **API Docs**: Scalar
- **Testing**: xUnit (plain assertions, no FluentAssertions)
- **CQRS**: Kommand (https://github.com/Atherio-Ltd/Kommand)

### Package Manager
- **npm** (not yarn or pnpm) for frontend
- **dotnet CLI** for backend

---

## Code Standards - ENFORCE STRICTLY

### 🚨 CRITICAL: Secrets Management & Security

**❌ NEVER COMMIT SECRETS TO SOURCE CONTROL**

This is a **non-negotiable security requirement**:

**What counts as a secret:**
- ❌ Connection strings (database, cache, etc.)
- ❌ API keys and tokens
- ❌ JWT secret keys
- ❌ OAuth client secrets
- ❌ Encryption keys
- ❌ Third-party service credentials
- ❌ Any password or sensitive configuration

**How to handle secrets:**

**.NET Backend:**
1. **Use .NET User Secrets for local development:**
   ```bash
   cd apps/backend/src/Codewrinkles.API
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
   dotnet user-secrets set "Jwt:SecretKey" "your-secret-key"
   ```

2. **Connection String Format (SQL Server LocalDB):**
   ```
   Server=(localdb)\mssqllocaldb;Database=codewrinkles;Trusted_Connection=True;MultipleActiveResultSets=true
   ```

3. **Use environment variables for production:**
   - Azure: App Configuration / Key Vault
   - Docker: Environment variables
   - CI/CD: Encrypted secrets

**Frontend:**
1. **Use `.env.local` (Git-ignored)** for local development
2. **NEVER commit `.env` files with real values**
3. **Use environment-specific configs** in CI/CD

**What CAN be committed:**
- ✅ Non-sensitive configuration (Issuer, Audience, timeouts)
- ✅ Feature flags
- ✅ Public URLs
- ✅ Default values (placeholders like `"your-api-key-here"`)

**Verification checklist:**
- ✅ `.gitignore` includes `.env.local`, `appsettings.*.local.json`
- ✅ `appsettings.json` has NO connection strings or secrets
- ✅ User secrets initialized for API project
- ✅ All secrets stored in user secrets or environment variables

---

### 🚨 CRITICAL: Library Usage Policy

**❌ NEVER USE LIBRARIES UNLESS IT'S THE ONLY FEASIBLE OPTION**

This is a **fundamental principle** for both frontend and backend:

- ❌ **NO FluentAssertions** - use plain xUnit assertions
- ❌ **NO FluentValidation** - write validation logic ourselves
- ❌ **NO popular convenience libraries** that save a few lines of code
- ❌ **NO libraries for simple utilities** (string manipulation, mediation, validation, etc.)
- ✅ **ALWAYS ASK BEFORE adding any new library or package**

**When a library IS acceptable:**
- Framework essentials (React, .NET, EF Core, etc.)
- Complex, well-tested infrastructure (OpenTelemetry, BCrypt, JWT)
- Features that would take weeks to build correctly (OAuth clients, etc.)

**Why this matters:**
- Keeps dependencies minimal
- Forces us to understand the code we write
- Reduces security surface area
- Avoids bloat and version conflicts

**BEFORE adding any package, ask yourself:**
1. Can I build this in < 1 day?
2. Does this require deep domain expertise?
3. Is this a critical security feature?

If all three are "no", **build it yourself**.

---

### 🚨 CRITICAL: C# Nullable Reference Types & Anti-Patterns

**❌ NEVER USE `null!` OR `string.Empty` TO SUPPRESS WARNINGS**

When using nullable reference types in C#, compiler warnings exist for a reason - they indicate real logic problems.

**Anti-Patterns (NEVER DO THIS):**
```csharp
// ❌ WRONG - Defeats the purpose of nullable reference types
public string Email { get; private set; } = null!;
public string Name { get; private set; } = string.Empty;
```

**The Correct Pattern for EF Core Entities:**

For EF Core entities with private parameterless constructors, use **targeted pragma suppression**:

```csharp
public string Email { get; private set; }
public string Name { get; private set; }
public Profile Profile { get; private set; }

// Private parameterless constructor for EF Core materialization only
// EF Core will populate all properties via reflection when loading from database
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
private Identity() { }
#pragma warning restore CS8618

// Public factory method for creating valid instances
public static Identity Create(string email, string passwordHash)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(email);
    ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

    return new Identity
    {
        Email = email.Trim().ToLowerInvariant(),
        // ... set all required properties
    };
}
```

**Why This Matters:**
- ✅ **Explicit**: Clear about what we're suppressing and why (only for EF Core constructor)
- ✅ **Scoped**: Warning is restored immediately after, catching real bugs elsewhere
- ✅ **Documented**: Comment explains why suppression is necessary
- ❌ Using `null!` hides real null reference bugs throughout the codebase
- ❌ Using `string.Empty` lies about the data and can cause subtle bugs

**Rules:**
1. ✅ Use `#pragma warning disable/restore CS8618` around EF Core constructors only
2. ✅ Add comments explaining why suppression is needed
3. ✅ Always restore the warning immediately after
4. ❌ **NEVER** use `null!` on property declarations
5. ❌ **NEVER** use `string.Empty` to suppress warnings
6. ❌ **NEVER** use global nullable disable - fix the issues properly

**References:**
- [Working with nullable reference types - EF Core](https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types)
- [Required members - C# feature specifications](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/required-members)

---

### C# Class Member Ordering

**ENFORCE STRICT ORDERING** for all class members:

1. **Fields and constants**
2. **Constructors** (public, private, static, non-static - any order within this section)
3. **Properties** (auto-properties, computed properties, navigation properties)
4. **Factory methods** (static Create/CreateFrom methods)
5. **Public methods**
6. **Private methods**

**Example:**
```csharp
public sealed class Identity
{
    // 1. Fields (if any)
    private const int MaxLoginAttempts = 5;

    // 2. Constructors
#pragma warning disable CS8618
    private Identity() { } // EF Core constructor
#pragma warning restore CS8618

    // 3. Properties
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public Profile Profile { get; private set; } // Navigation property

    // 4. Factory methods
    public static Identity Create(string email, string passwordHash)
    {
        // ...
    }

    // 5. Public methods
    public void MarkEmailAsVerified()
    {
        // ...
    }

    // 6. Private methods
    private bool IsValidEmail(string email)
    {
        // ...
    }
}
```

**Why this order:**
- ✅ **Consistent**: Same structure across all classes makes code easier to navigate
- ✅ **Logical**: Data (fields/properties) before behavior (methods)
- ✅ **Public API first**: Constructors and factory methods (entry points) before implementation details

---

### 🚨 CRITICAL: GUID Primary Keys - Sequential Generation

**❌ NEVER USE `Guid.NewGuid()` FOR PRIMARY KEYS**

Random GUIDs cause severe database performance problems:
- ❌ **Massive index fragmentation** (can reach 99%+)
- ❌ **Poor insert performance** due to constant page splits
- ❌ **Wasted disk space** from fragmentation
- ❌ **Slower queries** due to fragmented data

**✅ ALWAYS let EF Core generate sequential GUIDs:**

```csharp
// ❌ WRONG - Random GUID causes index fragmentation
public static Identity Create(string email)
{
    return new Identity
    {
        Id = Guid.NewGuid(), // DON'T DO THIS
        Email = email
    };
}

// ✅ CORRECT - Let EF Core generate sequential GUID
public static Identity Create(string email)
{
    return new Identity
    {
        // Id will be generated by EF Core using sequential GUID generation
        Email = email
    };
}
```

**EF Core Configuration:**
```csharp
public void Configure(EntityTypeBuilder<Identity> builder)
{
    builder.HasKey(i => i.Id);

    // Explicitly configure sequential GUID generation
    // This avoids index fragmentation issues with random GUIDs
    builder.Property(i => i.Id)
        .ValueGeneratedOnAdd(); // EF Core uses SequentialGuidValueGenerator
}
```

**Why Sequential GUIDs:**
- ✅ **Minimal fragmentation**: Sequential values are added to end of index
- ✅ **Better insert performance**: No page splits required
- ✅ **Lower disk usage**: Pages remain compact
- ✅ **Faster queries**: Data is well-organized

**Technical Details:**
- EF Core SQL Server provider uses `SequentialGuidValueGenerator` by default for GUID primary keys
- This is similar to SQL Server's `NEWSEQUENTIALID()` function
- Values are generated **client-side** (no extra database round-trip)
- Still globally unique, but with sequential ordering

**Rules:**
1. ✅ **NEVER** set `Id = Guid.NewGuid()` in factory methods
2. ✅ Configure `ValueGeneratedOnAdd()` for all GUID primary keys
3. ✅ Add comment explaining sequential GUID generation
4. ❌ **NEVER** use `HasDefaultValueSql("NEWID()")` - this creates random GUIDs

**References:**
- [GUIDs as PRIMARY KEYs - Kimberly L. Tripp](https://www.sqlskills.com/blogs/kimberly/guids-as-primary-keys-andor-the-clustering-key/)
- [SQL Server GUID Index Fragmentation - MSSQLTips](https://www.mssqltips.com/sqlservertip/6595/sql-server-guid-column-and-index-fragmentation/)
- [EF Core Value Generation - Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/modeling/generated-properties)

---

### 🚨 CRITICAL: DateTime vs DateTimeOffset - ALWAYS Use DateTimeOffset

**❌ NEVER USE `DateTime` - ALWAYS USE `DateTimeOffset`**

This is a **non-negotiable standard** for all timestamp handling in the backend. `DateTime` loses timezone information, causing bugs when data crosses timezone boundaries.

**Anti-Pattern (NEVER DO THIS):**
```csharp
// ❌ WRONG - DateTime loses timezone context
public DateTime CreatedAt { get; private set; }
public DateTime? UpdatedAt { get; private set; }
CreatedAt = DateTime.UtcNow;
```

**The Correct Pattern:**
```csharp
// ✅ CORRECT - DateTimeOffset preserves timezone offset
public DateTimeOffset CreatedAt { get; private set; }
public DateTimeOffset? UpdatedAt { get; private set; }
CreatedAt = DateTimeOffset.UtcNow;
```

**EF Core Configuration:**
```csharp
// ✅ Use SYSDATETIMEOFFSET() for database defaults
builder.Property(e => e.CreatedAt)
    .IsRequired()
    .HasDefaultValueSql("SYSDATETIMEOFFSET()");

// ❌ NEVER use GETUTCDATE() - it returns datetime2, not datetimeoffset
```

**SQL Server Column Type:**
- `DateTimeOffset` in C# → `datetimeoffset` in SQL Server
- `DateTime` in C# → `datetime2` in SQL Server (loses timezone)

**Why DateTimeOffset:**
- ✅ **Preserves timezone offset**: UTC offset is stored with the value
- ✅ **Unambiguous**: No confusion about local vs UTC
- ✅ **API friendly**: JSON serialization includes offset (+00:00)
- ✅ **Cross-timezone safe**: Works correctly across server/client timezones
- ❌ `DateTime` loses context and causes subtle timezone bugs

**JWT Tokens Exception:**
The `JwtSecurityToken` class requires `DateTime`. Use `.UtcDateTime` to extract:
```csharp
// JWT token requires DateTime, so extract from DateTimeOffset
var token = new JwtSecurityToken(
    expires: DateTimeOffset.UtcNow.AddMinutes(15).UtcDateTime,
    // ...
);
```

**Rules:**
1. ✅ **ALWAYS** use `DateTimeOffset` for all timestamp properties
2. ✅ **ALWAYS** use `DateTimeOffset.UtcNow` instead of `DateTime.UtcNow`
3. ✅ **ALWAYS** use `SYSDATETIMEOFFSET()` for SQL Server defaults
4. ❌ **NEVER** use `DateTime` in domain entities or DTOs
5. ❌ **NEVER** use `GETUTCDATE()` in EF Core configurations
6. ⚠️ JWT tokens are the ONLY exception (use `.UtcDateTime`)

**References:**
- [DateTimeOffset vs DateTime - Stack Overflow](https://stackoverflow.com/questions/4331189/datetime-vs-datetimeoffset)
- [Best Practices for DateTimeOffset - Microsoft](https://learn.microsoft.com/en-us/dotnet/standard/datetime/choosing-between-datetime)

---

### 🚨 CRITICAL: C# Async/Await Patterns

**✅ ALWAYS USE `async`/`await` - NEVER USE `.AsTask()` OR OTHER WORKAROUNDS**

This is a **non-negotiable standard** for all asynchronous code in the backend.

**Anti-Pattern (NEVER DO THIS):**
```csharp
// ❌ WRONG - Using .AsTask() to convert ValueTask to Task
public Task<Profile?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return _profiles.FindAsync([id], cancellationToken: cancellationToken).AsTask();
}

// ❌ WRONG - Returning task directly without async/await
public Task<Identity?> FindByIdAsync(Guid id)
{
    return _identities.FindAsync([id]).AsTask();
}
```

**The Correct Pattern:**
```csharp
// ✅ CORRECT - Standard async/await pattern
public async Task<Profile?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await _profiles.FindAsync([id], cancellationToken: cancellationToken);
}

// ✅ CORRECT - Compiler handles ValueTask<T> → Task<T> conversion automatically
public async Task<Identity?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    return await _identities.FindAsync([id], cancellationToken: cancellationToken);
}
```

**Why This Matters:**
- ✅ **Idiomatic C#**: async/await is the standard, readable pattern
- ✅ **Compiler optimized**: The compiler handles conversions efficiently
- ✅ **Consistent**: Same pattern across the entire codebase
- ✅ **Debuggable**: Stack traces are cleaner with async/await
- ✅ **Maintainable**: Other developers expect async/await, not workarounds
- ❌ `.AsTask()` is a code smell indicating unnecessary manual conversion
- ❌ Non-standard patterns make code harder to understand and maintain

**Technical Note:**
- EF Core methods like `FindAsync()` return `ValueTask<T>` for performance
- When your interface requires `Task<T>`, use `async`/`await`
- The compiler automatically converts `ValueTask<T>` to `Task<T>`
- **NO manual conversion needed** - async/await handles it

**Rules:**
1. ✅ **ALWAYS** use `async`/`await` for asynchronous methods
2. ✅ **ALWAYS** include `CancellationToken` parameter (defaults to `default`)
3. ❌ **NEVER** use `.AsTask()` - use async/await instead
4. ❌ **NEVER** use other workarounds like `Task.FromResult()` for async methods
5. ❌ **NEVER** return tasks directly without await (except fire-and-forget scenarios)
6. ⚠️ **Any exceptions to this rule must be discussed and approved first**

**When Exceptions Might Apply (rare, require approval):**
- Performance-critical hot paths where async state machine overhead is measured and significant
- Implementing interface methods that require synchronous execution
- Fire-and-forget scenarios (even then, use `_ = Task.Run(...)` pattern)

**Before using any alternative pattern:**
1. Measure and prove the performance impact
2. Document why async/await is insufficient
3. Get explicit approval from the team

---

### 🚨 CRITICAL: Database Transactions

**Use explicit transactions when multiple entities must succeed or fail together.**

**When to Use Transactions:**

| Scenario | Use Transaction? | Reason |
|----------|------------------|--------|
| Creating related entities (Identity + Profile) | ✅ Yes | Both must exist or neither |
| Creating entity + related child (Pulse + Engagement) | ✅ Yes | Child is required, can't have orphan parent |
| Operations with external side effects (upload + DB) | ✅ Yes | Need to clean up external resource on rollback |
| Single entity CRUD | ❌ No | `SaveChangesAsync` is already atomic |
| Read-only queries | ❌ No | No state changes |

**The Transaction Pattern:**

```csharp
// From RegisterUser.cs - Creating Identity and Profile atomically
await using var transaction = await _unitOfWork.BeginTransactionAsync(
    IsolationLevel.ReadCommitted,
    cancellationToken);
try
{
    // 1. Create first entity
    var identity = Identity.Create(email, passwordHash);
    _unitOfWork.Identities.Register(identity);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    // identity.Id is now available (generated by EF Core)

    // 2. Create second entity that depends on first
    var profile = Profile.Create(identityId: identity.Id, name, handle);
    _unitOfWork.Profiles.Create(profile);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    // 3. Commit - both Identity and Profile now persisted
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    // Rollback - neither Identity nor Profile will exist
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

**Key Points:**

1. **`await using var transaction`** - Ensures disposal even if exception occurs
2. **Multiple `SaveChangesAsync` calls** - Each one writes to DB but is not final until Commit
3. **Explicit `CommitAsync`** - Makes all changes permanent
4. **Explicit `RollbackAsync` in catch** - Reverts all changes on any error
5. **Re-throw after rollback** - Let the error propagate after cleanup

**Handling External Side Effects:**

When your transaction involves external resources (blob storage, email, etc.), clean up on rollback:

```csharp
// From CreatePulse.cs - Blob upload with transaction
string? imageUrl = null;

await using var transaction = await _unitOfWork.BeginTransactionAsync(
    IsolationLevel.ReadCommitted,
    cancellationToken);
try
{
    // Create Pulse
    _unitOfWork.Pulses.Create(pulse);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    // Upload image (external side effect)
    if (command.ImageStream is not null)
    {
        var (url, width, height) = await _blobStorageService.UploadPulseImageAsync(
            command.ImageStream,
            pulse.Id,
            cancellationToken);
        imageUrl = url;

        // Save image record
        var pulseImage = PulseImage.Create(pulse.Id, url, width, height);
        _unitOfWork.Pulses.CreateImage(pulseImage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);

    // Clean up external resource that was created
    if (imageUrl is not null)
    {
        try
        {
            await _blobStorageService.DeletePulseImageAsync(pulse.Id, cancellationToken);
        }
        catch
        {
            // Log but don't throw - cleanup is best effort
        }
    }

    throw;
}
```

**Isolation Levels:**

| Level | Use Case |
|-------|----------|
| `ReadCommitted` | Default - prevents dirty reads, sufficient for most operations |
| `RepeatableRead` | Rarely needed - when you read data and later update based on it |
| `Serializable` | Avoid - high contention, only for critical financial operations |

**Rules:**

1. ✅ **ALWAYS** use transactions when creating multiple related entities
2. ✅ **ALWAYS** use `await using` for automatic disposal
3. ✅ **ALWAYS** explicitly call `CommitAsync` on success
4. ✅ **ALWAYS** explicitly call `RollbackAsync` in catch block
5. ✅ **ALWAYS** clean up external resources on rollback
6. ✅ **ALWAYS** re-throw after rollback (let error propagate)
7. ❌ **NEVER** swallow exceptions in transaction catch blocks
8. ❌ **NEVER** use transactions for single-entity operations
9. ⚠️ Default to `IsolationLevel.ReadCommitted` unless you have a specific reason

**References:**
- `RegisterUser.cs` - Identity + Profile transaction
- `CreatePulse.cs` - Pulse + Engagement + Image + Mentions transaction with blob cleanup

---

### TypeScript
- ✅ Strict mode enabled in `tsconfig.json`
- ✅ ALL strictness flags enabled (strictNullChecks, noImplicitAny, etc.)
- ❌ **NEVER use `any` type** - use `unknown` or proper types
- ❌ **NEVER use `@ts-ignore`** - fix the type error instead
- ✅ Explicit return types on functions
- ✅ Use type imports: `import type { Foo } from "./types"`

### React
- ✅ Use functional components (no class components)
- ✅ Use hooks (`useState`, `useEffect`, etc.)
- ✅ JSX return type: `JSX.Element`
- ❌ **NO `import React from "react"`** - we use the new JSX transform
- ✅ Props interfaces named `{ComponentName}Props`
- ✅ Export component function directly: `export function ComponentName()`

### File Naming
- ✅ Components: `PascalCase.tsx` (e.g., `LoginPage.tsx`, `UserProfile.tsx`)
- ✅ Hooks: `camelCase.ts` with `use` prefix (e.g., `useTheme.ts`, `useAuth.ts`)
- ✅ Types: `types.ts` (shared types in `src/types.ts`, feature-specific in feature folder)
- ✅ Utilities: `camelCase.ts` (e.g., `formatDate.ts`)

### Imports
- ✅ Group imports: React → third-party → types → local components → hooks → utils
- ✅ Use relative imports for local files
- ✅ Barrel exports OK for features (`index.ts` re-exporting components)

---

## Project Structure

```
apps/frontend/
├── src/
│   ├── features/           # Feature-based organization (PRIMARY pattern)
│   │   ├── auth/          # Login, Register, AuthCard, etc.
│   │   ├── home/          # HomePage and related components
│   │   ├── pulse/         # Microblogging feature
│   │   ├── learn/         # Learning feature (will merge into Nova)
│   │   ├── twin/          # AI Twin feature (will merge into Nova)
│   │   ├── settings/      # Settings page and sections
│   │   ├── shell/         # App shell (Header, Nav, Layout)
│   │   └── onboarding/    # Onboarding flow
│   ├── components/        # Shared UI components only
│   │   └── ui/            # Button, Card, Toggle, Tag, etc.
│   ├── hooks/             # Shared hooks (useTheme, etc.)
│   ├── types.ts           # Shared type definitions
│   ├── App.tsx            # Main app with routing
│   └── main.tsx           # Entry point
├── public/                # Static assets
├── package.json
├── tsconfig.json          # TypeScript config (strict mode)
├── tailwind.config.js     # Design tokens
├── vite.config.ts
└── postcss.config.js
```

### When to Create a New Feature Folder
- Feature has 2+ components
- Feature has its own routing
- Feature has self-contained logic
- Examples: `auth/`, `pulse/`, `settings/`

### When to Use `components/ui/`
- Component is reusable across multiple features
- Component is purely presentational (no business logic)
- Examples: `Button`, `Card`, `Toggle`, `Tag`

---

## Routing Patterns

### React Router v7
- ✅ Use `BrowserRouter` in `App.tsx`
- ✅ Use `<Routes>` and `<Route>` for route definitions
- ✅ Use `<Outlet>` for nested routes (see `ShellLayout`, `SettingsPage`)
- ✅ Use `<Link>` for navigation
- ✅ Use `<NavLink>` for nav items (has `isActive` prop)
- ✅ Use `useNavigate()` for programmatic navigation
- ✅ Use `useLocation()` to access current route

### Route Structure
```
/ → Home
/social → Pulse (microblogging)
/learn → Learn (will become /nova)
/twin → Twin (will become /nova)
/settings → Settings
  /settings/profile
  /settings/account
  /settings/apps
  /settings/notifications
/login → Login
/register → Register
/onboarding → Onboarding
```

### Nested Routes Pattern
See `SettingsPage.tsx` and `App.tsx` for reference:
- Parent route renders `<Outlet />`
- Child routes render inside Outlet
- Use `<Navigate>` for default redirects

---

## Design System

### Colors (Tailwind Custom Tokens)
```javascript
brand: {
  DEFAULT: "#20C1AC",  // Primary brand color
  soft: "#35D6C0"      // Softer variant
}

surface: {
  page: "#050505",     // Page background
  card1: "#0A0A0A",    // Card background (elevated)
  card2: "#111111"     // Card background (more elevated)
}

border: {
  DEFAULT: "#2A2A2A",  // Default borders
  deep: "#1A1A1A"      // Deeper borders
}

text: {
  primary: "#F3F4F6",    // Main text
  secondary: "#A1A1AA",  // Secondary text
  tertiary: "#737373"    // Tertiary text (subtle)
}
```

### Usage in Components
- ✅ Use Tailwind classes with these tokens: `bg-surface-card1`, `text-text-primary`, `border-border`
- ✅ Consistent spacing: `space-y-6`, `gap-3`, `p-4`, `px-3 py-2`
- ✅ Consistent rounding: `rounded-xl` for cards, `rounded-full` for buttons/tags
- ✅ Consistent borders: `border border-border`

### Typography Scale
- Page title: `text-base font-semibold tracking-tight`
- Section title: `text-sm font-semibold tracking-tight`
- Body text: `text-sm text-text-primary`
- Secondary text: `text-xs text-text-secondary`
- Tertiary/muted: `text-xs text-text-tertiary`
- Tags/badges: `text-[11px]`

---

## Component Patterns

### Basic Component Template
```tsx
import type { SomeType } from "../../types";

export interface ComponentNameProps {
  someProp: string;
  optionalProp?: number;
}

export function ComponentName({ someProp, optionalProp }: ComponentNameProps): JSX.Element {
  return (
    <div className="...">
      {/* component content */}
    </div>
  );
}
```

### Hooks Pattern
```tsx
import { useState, useEffect } from "react";

export function useCustomHook() {
  const [state, setState] = useState<Type>(initialValue);

  useEffect(() => {
    // side effects
  }, []);

  return { state, setState };
}
```

### Event Handlers
- ✅ Use explicit types: `(e: React.FormEvent): void => {}`
- ✅ Name handlers: `handleSubmit`, `handleClick`, `handleChange`
- ✅ For callbacks passed as props: `onSubmit`, `onClick`, `onChange`

---

## State Management

### Current Approach
- ✅ Local state: `useState` for component-level state
- ✅ Shared state: Custom hooks (see `useTheme.ts`)
- ✅ Theme persistence: `localStorage` in `useTheme` hook
- ❌ NO global state library yet (Redux, Zustand, etc.) - use hooks for now

### Future (when needed)
- User authentication state → Context or lightweight state library
- Global UI state (modals, toasts) → Context
- Server state → React Query or SWR

---

## Styling Rules

### TailwindCSS Usage
- ✅ Use utility classes directly in JSX
- ✅ Conditional classes: Template literals with ternary
  ```tsx
  className={`base-classes ${isActive ? "active-classes" : "inactive-classes"}`}
  ```
- ✅ For complex conditionals: Extract to variable
- ❌ NO inline styles - use Tailwind classes
- ❌ NO custom CSS files (except `index.css` for Tailwind directives)

### Responsive Design
- ✅ Mobile-first approach
- ✅ Use Tailwind breakpoints: `sm:`, `md:`, `lg:`
- ✅ Hide on mobile: `hidden lg:flex`
- ✅ Show on mobile only: `lg:hidden`

---

## Common Commands

```bash
# Development
cd apps/frontend
npm run dev         # Start dev server (http://localhost:5173)

# Type checking
npm run lint        # Run TypeScript compiler (no emit)

# Build
npm run build       # TypeScript compile + Vite build
npm run preview     # Preview production build

# Install dependencies
npm install         # Install all dependencies
```

---

## Anti-Patterns - DO NOT DO

❌ **NO `any` types** - always use proper types or `unknown`
❌ **NO `@ts-ignore`** - fix the actual type error
❌ **NO `import React from "react"`** - unnecessary with new JSX transform
❌ **NO inline styles** - use Tailwind classes
❌ **NO custom CSS files** (except necessary global styles)
❌ **NO class components** - use functional components
❌ **NO prop drilling beyond 2 levels** - use context or hooks
❌ **NO magic numbers** - extract to constants
❌ **NO console.log in production code** - remove before committing
❌ **NO over-engineering** - keep it simple, iterate based on needs

---

## Best Practices

### Code Organization
- ✅ One component per file
- ✅ Co-locate related components in feature folders
- ✅ Keep components small (<200 lines)
- ✅ Extract complex logic to hooks
- ✅ Extract reusable UI to `components/ui/`

### Type Safety
- ✅ Define props interfaces for all components
- ✅ Use union types for variants: `type Variant = "primary" | "secondary"`
- ✅ Use `as const` for readonly arrays/objects
- ✅ Prefer interfaces over types for objects (better error messages)

### Performance
- ✅ Use React.memo() only when needed (avoid premature optimization)
- ✅ Use useCallback/useMemo for expensive computations
- ❌ Don't optimize until you have a performance problem

### Accessibility
- ✅ Use semantic HTML (`<button>`, `<nav>`, `<main>`, etc.)
- ✅ Add `aria-label` for icon-only buttons
- ✅ Ensure keyboard navigation works
- ✅ Use proper heading hierarchy (h1 → h2 → h3)

---

## Git Workflow

### Branches
- `main` - production-ready code
- Feature branches: `feature/feature-name`
- Bug fixes: `fix/bug-description`

### Commits
- ✅ Descriptive commit messages
- ✅ Conventional commits format (optional): `feat:`, `fix:`, `refactor:`, etc.
- ✅ Commit working code (passes TypeScript + builds)

---

## Backend Architecture (Decided)

### Structure
- **Location**: `apps/backend/`
- **Solution**: `Codewrinkles.sln`
- **Projects**:
  - `Codewrinkles.API` - Minimal APIs, endpoints, middleware
  - `Codewrinkles.Application` - Use cases, Kommand handlers
  - `Codewrinkles.Domain` - Entities, value objects, domain events
  - `Codewrinkles.Infrastructure` - EF Core, DbContext, external services

### Patterns
- ✅ Clean Architecture (layered monolith)
- ✅ Single database with schema-per-module (`identity`, `pulse`, `nova`)
- ✅ Single `ApplicationDbContext` with all DbSets
- ✅ Modules as folders (not separate projects)
- ✅ Feature-based organization (NOT convention-based like Commands/Queries folders)
- ✅ CQRS with Kommand
- ✅ OpenTelemetry instrumentation from the start
- ✅ Custom authentication (no ASP.NET Core Identity)
- ✅ JWT + refresh tokens with revocation

### 🚨 CRITICAL: CQRS Handler Reuse - NEVER REUSE

**❌ NEVER REUSE QUERIES, COMMANDS, OR HANDLERS**

This is a **fundamental architectural rule** that prevents spaghetti code:

**The Iron Rule:**
> For any specific action in the app, there is always **exactly 1 handler** that executes with its corresponding request and response records.

**NEVER reuse in these scenarios:**

1. ❌ **NOT in middleware**
   ```csharp
   // ❌ WRONG - Middleware reusing existing query
   public async Task InvokeAsync(HttpContext context, IMediator mediator)
   {
       var query = new GetPulseQuery(pulseId); // Reusing GetPulse
       var result = await mediator.SendAsync(query);
   }

   // ✅ CORRECT - Create dedicated query for middleware use case
   public async Task InvokeAsync(HttpContext context, IMediator mediator)
   {
       var query = new GetPulseHtmlQuery(pulseId); // New query for SEO
       var result = await mediator.SendAsync(query);
   }
   ```

2. ❌ **NOT in API endpoints**
   ```csharp
   // ❌ WRONG - Endpoint reusing query from another feature
   app.MapGet("/admin/users", async (IMediator mediator) =>
   {
       var query = new GetUsersQuery(); // Reusing from user feature
       return await mediator.SendAsync(query);
   });

   // ✅ CORRECT - Create dedicated query for admin endpoint
   app.MapGet("/admin/users", async (IMediator mediator) =>
   {
       var query = new GetUsersForAdminQuery(); // Admin-specific query
       return await mediator.SendAsync(query);
   });
   ```

3. ❌ **NOT calling handler from another handler**
   ```csharp
   // ❌ WRONG - Handler calling another handler via IMediator
   public sealed class CreatePulseCommandHandler : ICommandHandler<CreatePulseCommand, CreatePulseResult>
   {
       public async Task<CreatePulseResult> HandleAsync(CreatePulseCommand command, CancellationToken ct)
       {
           // Create pulse logic...

           // ❌ Calling another handler
           var query = new GetProfileQuery(command.AuthorId);
           var profile = await _mediator.SendAsync(query, ct);
       }
   }

   // ✅ CORRECT - Use repositories directly via IUnitOfWork
   public sealed class CreatePulseCommandHandler : ICommandHandler<CreatePulseCommand, CreatePulseResult>
   {
       public async Task<CreatePulseResult> HandleAsync(CreatePulseCommand command, CancellationToken ct)
       {
           // Create pulse logic...

           // ✅ Use repository directly
           var profile = await _unitOfWork.Profiles.FindByIdAsync(command.AuthorId, ct);
       }
   }
   ```

**Why This Rule Exists:**

- ✅ **Single Responsibility**: Each handler serves exactly one use case
- ✅ **Independent Evolution**: Handlers can change without affecting others
- ✅ **Clear Boundaries**: Easy to understand what each handler does
- ✅ **No Coupling**: Handlers don't depend on each other
- ✅ **Prevents Spaghetti**: No tangled web of handler dependencies
- ❌ Reusing handlers creates hidden coupling and cascading changes
- ❌ "It seems like duplication" is NOT a valid reason to reuse
- ❌ Different code blocks have different reasons for change (even if similar)

**What Looks Like Duplication (But Isn't):**

```csharp
// These two handlers look similar, but serve different use cases
// GetPulse: For API endpoint returning JSON
public sealed class GetPulseQueryHandler { /* ... */ }

// GetPulseHtml: For SEO bots returning HTML
public sealed class GetPulseHtmlQueryHandler { /* ... */ }

// They have DIFFERENT:
// - Response types (PulseDto vs HTML string)
// - Data requirements (full engagement vs minimal data)
// - Validation rules (user permissions vs public access)
// - Reasons for change (API contract vs SEO requirements)
```

**Shared Logic:**

If multiple handlers need the same logic, extract it to:
- ✅ **Domain methods** (on entities)
- ✅ **Application services** (in Application layer)
- ✅ **Repository methods** (for data access)

```csharp
// ✅ CORRECT - Shared logic in service
public sealed class HtmlGenerationService
{
    public string GeneratePulseHtml(PulseHtmlData data) { /* ... */ }
}

// Multiple handlers can use the service
public sealed class GetPulseHtmlQueryHandler
{
    public async Task<GetPulseHtmlResult> HandleAsync(/* ... */)
    {
        var html = _htmlGenerator.GeneratePulseHtml(data); // Shared service
    }
}

public sealed class GetProfileHtmlQueryHandler
{
    public async Task<GetProfileHtmlResult> HandleAsync(/* ... */)
    {
        var html = _htmlGenerator.GenerateProfileHtml(data); // Shared service
    }
}
```

**Rules:**

1. ❌ **NEVER** reuse queries/commands/handlers across different use cases
2. ❌ **NEVER** call a handler from another handler via IMediator
3. ❌ **NEVER** reuse handlers in middleware or endpoints
4. ✅ **ALWAYS** create a new query/command/handler for each distinct action
5. ✅ **ALWAYS** use repositories directly (via IUnitOfWork) when handlers need data
6. ✅ **ALWAYS** extract shared logic to services, not by reusing handlers
7. ⚠️ When in doubt: Create a new handler. "Duplication" is better than coupling.

**Exception:**

The ONLY time you can reuse a query/command is when calling it from the **exact same context** (e.g., same endpoint retrying, same middleware on different request). Even then, prefer a new query.

---

### 🚨 CRITICAL: Kommand Validator/Handler Pattern

**Validators validate. Handlers orchestrate.**

This is a **strict separation of concerns**:

- **Validators**: ALL precondition checks (entity exists, account state, uniqueness, credentials)
- **Handlers**: ONLY orchestrate the action after validation passes (modify state, generate tokens, etc.)

**Validator Responsibilities:**
```csharp
public sealed class LoginUserValidator : IValidator<LoginUserCommand>
{
    // ✅ Input format validation (email format, required fields, etc.)
    // ✅ Application-level validation (entity exists, account active, not locked)
    // ✅ Business rule validation (uniqueness checks, credentials verification*)

    // *Exception: If credential verification has SIDE EFFECTS (like recording
    // failed login attempts), keep it in handler
}
```

**Handler Responsibilities:**
```csharp
public sealed class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, LoginUserResult>
{
    // Validator has already confirmed all preconditions
    // Handler ONLY does:
    // ✅ Fetch entities (guaranteed to exist after validation)
    // ✅ Execute the action (modify state, create records, etc.)
    // ✅ Handle operations with side effects (e.g., password verification that records failed attempts)
    // ✅ Generate results (tokens, DTOs, etc.)
}
```

**Validator Structure:**
```csharp
public async Task<ValidationResult> ValidateAsync(
    TCommand request,
    CancellationToken cancellationToken)
{
    _errors = [];

    // 1. Input format validation (synchronous)
    ValidateRequiredFields(request);
    ValidateFormats(request);

    // If basic validation fails, return early
    if (_errors.Count > 0)
    {
        return ValidationResult.Failure(_errors);
    }

    // 2. Application-level validation (async, requires database)
    await ValidateEntityExistsAsync(request, cancellationToken);
    await ValidateBusinessRulesAsync(request, cancellationToken);

    return _errors.Count > 0
        ? ValidationResult.Failure(_errors)
        : ValidationResult.Success();
}
```

**Error Handling in Validators:**
- Return `ValidationResult.Failure(_errors)` for input format errors
- Throw domain exceptions for application-level failures (e.g., `ProfileNotFoundException`, `AccountLockedException`)

**Example - Login Validation:**
```csharp
// In validator:
// ✅ Check email format → ValidationError
// ✅ Check password not empty → ValidationError
// ✅ Check identity exists → throw InvalidCredentialsException
// ✅ Check account active → throw AccountSuspendedException
// ✅ Check not locked out → throw AccountLockedException

// In handler:
// ✅ Verify password (has side effect: records failed attempts)
// ✅ Record successful login
// ✅ Generate JWT tokens
```

**Why This Matters:**
- ✅ **Clear separation**: Validation logic is testable in isolation
- ✅ **Handler simplicity**: Handlers assume preconditions are met
- ✅ **Consistent pattern**: Every command/query follows the same structure
- ✅ **Fail fast**: Invalid requests are rejected before expensive operations

**Rules:**
1. ✅ **ALWAYS** validate entity existence in validator
2. ✅ **ALWAYS** validate business rules (uniqueness, state) in validator
3. ✅ Keep operations with side effects in handler
4. ❌ **NEVER** duplicate validation between validator and handler
5. ❌ **NEVER** check entity existence in handler (validator already did)

### Repository Pattern

**Cache primary DbSet, access others via DbContext**

Repositories cache their primary DbSet for convenience but access other DbSets via `_context.Set<T>()` when needed.

**The Pattern:**
```csharp
// ✅ CORRECT - Cache primary DbSet, access others via _context
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

    public async Task<IReadOnlyList<Profile>> GetMostFollowedProfilesAsync(int limit, CancellationToken ct)
    {
        // When you need another DbSet, access via _context.Set<T>()
        var follows = _context.Set<Follow>();

        return await _profiles
            .AsNoTracking()
            .GroupJoin(follows, p => p.Id, f => f.FollowingId, (p, fg) => new { p, Count = fg.Count() })
            .OrderByDescending(x => x.Count)
            .Take(limit)
            .Select(x => x.p)
            .ToListAsync(ct);
    }
}
```

**Rules:**
1. ✅ Cache the primary DbSet for the repository's main entity
2. ✅ Access other DbSets via `_context.Set<T>()` when needed
3. ✅ Use `AsNoTracking()` for read-only queries
4. ✅ Use async/await pattern consistently

### OpenAPI & API Documentation

**Built-in OpenAPI + Scalar (NOT Swashbuckle)**

We use **.NET 10's built-in OpenAPI** with **Scalar** for API documentation.

**Configuration (in Program.cs):**
```csharp
// Register OpenAPI document generation
builder.Services.AddOpenApi();

// In development, map OpenAPI endpoint and Scalar UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();           // Exposes /openapi/v1.json
    app.MapScalarApiReference(); // Scalar UI at /scalar
}
```

**Endpoint Registration:**
```csharp
// ✅ CORRECT - Minimal API endpoints are auto-discovered
app.MapPost("/api/users", CreateUser)
    .WithName("CreateUser")
    .WithTags("Users");

// ❌ WRONG - WithOpenApi() is DEPRECATED
app.MapPost("/api/users", CreateUser)
    .WithOpenApi(); // DON'T USE THIS - ASPDEPR002
```

**Customizing OpenAPI Metadata (if needed):**
```csharp
app.MapPost("/api/users", CreateUser)
    .WithName("CreateUser")
    .WithTags("Users")
    .AddOpenApiOperationTransformer((operation, context, ct) =>
    {
        operation.Summary = "Creates a new user";
        operation.Description = "Registers a user with email and password";
        return Task.CompletedTask;
    });
```

**Rules:**
1. ✅ Use `AddOpenApi()` for document generation
2. ✅ Use `MapScalarApiReference()` for UI
3. ✅ Use `.WithName()` and `.WithTags()` for basic metadata
4. ✅ Use `.AddOpenApiOperationTransformer()` for advanced customization
5. ❌ **NEVER** use `.WithOpenApi()` - it's deprecated (ASPDEPR002)
6. ❌ **NEVER** install Swashbuckle packages

**Why This Matters:**
- ✅ Built-in support is faster and lighter than Swashbuckle
- ✅ Works with AOT compilation
- ✅ Scalar provides better UI than Swagger UI
- ✅ No third-party dependencies for OpenAPI

**References:**
- [Breaking change: Deprecation of WithOpenApi](https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/10/withopenapi-deprecated)
- [Use OpenAPI documents](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0)

### Future Decisions (Not Implemented Yet)

#### Testing (Frontend)
- Unit tests: TBD (Vitest or Jest)
- Component tests: TBD (React Testing Library)
- E2E tests: TBD (Playwright or Cypress)

#### Databases (Future modules)
- Vector DB: TBD (Pinecone, Weaviate, Qdrant for Nova)

### CI/CD
- TBD (GitHub Actions, Vercel, etc.)

---

## Quick Debugging Tips

### TypeScript Errors
- Run `npm run lint` to see all type errors
- Check `tsconfig.json` - strict mode is enabled
- Look for missing type imports

### Build Errors
- Check for console.logs or comments with syntax errors
- Verify all imports are correct
- Run `npm run build` to see detailed errors

### Runtime Errors
- Check browser console (dev server runs on http://localhost:5173)
- Verify React Router routes are correct
- Check for missing prop types

---

## Notes for Claude Sessions

1. **Always read `core.md` first** to understand product vision
2. **Always read this file** to understand technical conventions
3. **🚨 ALWAYS ASK before adding any library/package** - see Library Usage Policy above
4. **Run `npm run lint` before finishing** (frontend) to ensure type safety
5. **Test the build** with `npm run build` (frontend) or `dotnet build` (backend) for production readiness
6. **Keep strict TypeScript** - no `any`, no `@ts-ignore`
7. **Feature folders** for new features, not flat component structure (frontend and backend)
8. **Design tokens** - use custom Tailwind colors, don't hardcode hex values (frontend)
9. **Mobile-first** - always consider responsive design (frontend)
10. **Ask before over-engineering** - keep it simple until complexity is needed
11. **Don't make assumptions** - ask questions when unclear
12. **Document decisions** - update this file if you make new technical decisions

---

## Current State Reminders

### Frontend
- ✅ React Router is fully integrated
- ✅ Theme system works (dark/light with localStorage)
- ✅ Settings has nested routes working
- ⚠️ Learn + Twin will merge into Nova (not refactored yet)
- ⚠️ No real authentication (UI only, no backend integration)
- ⚠️ No API calls yet
- ⚠️ No tests yet

### Backend
- ✅ Project structure defined in `backend.md`
- ✅ Solution and projects created
- ✅ NuGet packages installed
- ⚠️ No database schema yet
- ⚠️ No features implemented yet
- ⚠️ Feature implementation will be done one at a time

---

## SEO & Meta Tags (Vercel Edge Function)

The frontend is hosted on Vercel and uses a **dual SEO system** to ensure social media crawlers see correct meta tags.

### Why Two Systems?

Social media crawlers (Twitter, Facebook, LinkedIn, etc.) don't execute JavaScript. They only see the initial HTML. Since we use React (client-side rendering), we need server-side meta tag injection for crawlers.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Request to codewrinkles.com              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│              Vercel Edge Function (api/inject-meta.ts)      │
│  - Detects bots via User-Agent                              │
│  - Injects meta tags into HTML template                     │
│  - Returns pre-rendered HTML for crawlers                   │
└─────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
┌──────────────────────┐        ┌──────────────────────┐
│     Bot/Crawler      │        │    Regular Browser   │
│  Sees: Server-side   │        │  Sees: React Helmet  │
│  injected meta tags  │        │  updates meta tags   │
└──────────────────────┘        └──────────────────────┘
```

### Key Files

| File | Purpose |
|------|---------|
| `apps/frontend/api/inject-meta.ts` | Edge function that injects meta tags for bots |
| `apps/frontend/api/index.template.ts` | Auto-generated HTML template with placeholders |
| `apps/frontend/index.html` | Source HTML with `__META_*__` placeholders |
| `apps/frontend/scripts/bundle-html.js` | Bundles built HTML into template (runs after build) |

### How It Works

1. **Build time**: Vite builds `index.html` → `dist/index.html` (with asset hashes)
2. **Post-build**: `bundle-html.js` converts `dist/index.html` → `api/index.template.ts`
3. **Runtime**: Edge function uses template, replaces `__META_*__` placeholders with actual values

### Meta Tag Placeholders

```html
__META_TITLE__        → Page title
__META_DESCRIPTION__  → Page description
__META_KEYWORDS__     → SEO keywords
__META_AUTHOR__       → Author name
__META_IMAGE__        → OG image URL
__META_URL__          → Canonical URL
__META_OG_TYPE__      → website | article | profile
__TWITTER_CARD__      → summary | summary_large_image
__STRUCTURED_DATA__   → JSON-LD structured data
```

### Static Page Meta Configuration

Static pages (landing, privacy, terms) have their meta defined in `STATIC_PAGE_META` object in `inject-meta.ts`:

```typescript
const STATIC_PAGE_META = {
  '/': {
    title: 'Codewrinkles - Your Personal Path to Technical Excellence',
    description: 'Codewrinkles Nova is an AI coach...',
    keywords: 'AI coach, technical excellence...',
  },
  '/privacy': { /* ... */ },
  '/terms': { /* ... */ },
};
```

### Dynamic Pages

- `/pulse/{id}` - Fetches pulse data from API, injects author/content
- `/pulse/u/{handle}` - Fetches profile data from API, injects name/bio

### Twitter Card Types

- **`summary`**: Small square image (use for pages without hero images)
- **`summary_large_image`**: Large rectangular image (use only when page has actual large image)

Current usage:
- Landing page: `summary`
- Pulse feed: `summary`
- Profile pages: `summary`
- Individual pulses: `summary_large_image` only if pulse has attached image

### When Updating SEO

**To update landing page SEO:**
1. Update `STATIC_PAGE_META['/']` in `api/inject-meta.ts` (for crawlers)
2. Update `<Helmet>` in `LandingPage.tsx` (for browsers)
3. Both must match!

**To add a new static page:**
1. Add entry to `STATIC_PAGE_META` in `inject-meta.ts`
2. Add `<Helmet>` tags in the React component

**After changing `index.html` placeholders:**
1. Run `npm run build` in `apps/frontend`
2. Run `node scripts/bundle-html.js` to regenerate template
3. Or manually update `api/index.template.ts` if only placeholder name changed

---

## Infrastructure Preferences

### Database
- ✅ **Use SQL Server LocalDB** (accessed via JetBrains Rider)
- ❌ **NO Docker** - Do not use Docker for SQL Server or any other services unless explicitly requested
- Connection String format: `Server=(localdb)\\mssqllocaldb;Database=Codewrinkles;Trusted_Connection=True;MultipleActiveResultSets=true`

### Containers & Deployment
- ❌ **NO Docker** unless explicitly requested by the user
- Local development uses native tools (Rider, VS, etc.)

---

## Strategic Decisions Log

> **Purpose**: Record key product/GTM decisions with reasoning so future Claude sessions maintain consistency.

### Nova Alpha Strategy (December 2025)

| Decision | Choice | Reasoning |
|----------|--------|-----------|
| Application form with friction | Yes | Filter for committed testers, quality over quantity |
| Personal emails to accepted users | Yes | Founder-led approach builds loyalty |
| Tagline | "Your Personal Path to Technical Excellence" | Professional, no ceiling, implies personalization |
| Two paths to Alpha | Apply OR earn via Pulse (15 posts/30 days) | Gamification drives Pulse engagement |
| Alpha access = Founding Member | Yes | `IsFoundingMember` persists for future perks |

### Nova Pricing (Planned, Not Yet Implemented)

| Tier | Price | Features | Reasoning |
|------|-------|----------|-----------|
| Free | $0 | 10 msgs/day, 7-day history | Acquisition funnel, lets users hit "A-ha" |
| Pro | $12/month | Unlimited, full memory, skill tracking | Covers AI costs + margin, affordable for individuals |
| Lifetime | $199 | Pro forever + Founding badge | ~17 months breakeven, upfront cash |
| Founding Members | $8/month locked | Lifetime discount for Alpha/early Beta | Rewards early adopters |

### Current Status (Update This)

- **Alpha launched**: December 2025
- **YouTube announced**: Not yet
- **Applications**: 18
- **Channels reached**: LinkedIn, X, Pulse (NOT YouTube, NOT Substack yet)

### GTM Approach

- YouTube is the primary audience (31k .NET developers)
- Education-first content, not sales pitches
- Founder-led personal outreach for early users
- Pulse is the owned community layer

---

## Instructions for Claude Sessions

1. **Read this file AND `docs/alpha-to-launch-roadmap.md`** at session start
2. **Do proper research before recommending anything as "good"**
   - If you haven't researched it, say so
   - Use WebSearch to validate GTM/pricing/strategy assumptions
   - Present confidence levels honestly ("I researched this" vs "this is my intuition")
3. **Don't flip-flop between sessions**
   - If something was recommended before, assume there was reasoning behind it
   - If you think it needs to change, explain what NEW information justifies the change
4. **Ask clarifying questions** before making assumptions
   - Example: "Have you announced on YouTube yet?" before analyzing conversion rates
5. **Update this log** when new strategic decisions are made
6. **Role**: Coach and mentor. Be thorough. Do the research. Don't guess.

---

**Last Updated**: 2025-12-20 (update this when making significant changes)
