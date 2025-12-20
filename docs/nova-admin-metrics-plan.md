# Nova Admin Metrics - Implementation Plan

> **Purpose**: Add a dropdown navigation for Nova in the admin dashboard with Submissions (existing) and Metrics (new) pages.

> **Created**: December 20, 2025

---

## 1. Current State

### Admin Navigation Structure

Current `AdminNavigation.tsx` has 5 flat nav items:
```
Dashboard → /admin
Alpha → /admin/alpha (AlphaApplicationsPage - manages submissions)
Users → /admin/users
Content → /admin/content (not implemented)
Settings → /admin/settings (not implemented)
```

### Existing Metrics

**Dashboard metrics** (`GetDashboardMetrics.cs`):
- `TotalUsers` - Total registered users
- `ActiveUsers` - Users active in last 30 days
- `TotalPulses` - Total pulses created

**No Nova-specific metrics exist** in the admin dashboard yet.

---

## 2. Target State

### New Navigation Structure

```
Dashboard → /admin
Nova ▼ (expandable dropdown)
  ├── Submissions → /admin/nova/submissions (renamed from /admin/alpha)
  └── Metrics → /admin/nova/metrics (NEW)
Users → /admin/users
Content → /admin/content
Settings → /admin/settings
```

### Metrics Definition

Based on research from [Instabug on Beta Testing Metrics](https://www.instabug.com/blog/beta-test-metrics), [Userpilot on Activation Metrics](https://userpilot.com/blog/activation-metrics-saas/), and [ProductLed on PLG Metrics](https://productled.com/blog/product-led-growth-metrics):

#### Activation Definition

A user is **activated** when they:
1. Have Nova access (`NovaAccess != None`)
2. Have 3 or more conversation sessions

This is simple, measurable, and represents the user reaching the core value of Nova.

#### Metrics to Display

**Application Funnel (5 metrics)**:
| Metric | Description | Calculation |
|--------|-------------|-------------|
| Total Applications | All submissions | COUNT(*) AlphaApplication |
| Pending | Awaiting review | WHERE Status = Pending |
| Accepted | Approved | WHERE Status = Accepted |
| Waitlisted | Deferred | WHERE Status = Waitlisted |
| Redeemed | Codes used | WHERE InviteCodeRedeemed = true |

**User Metrics (3 metrics)**:
| Metric | Description | Calculation |
|--------|-------------|-------------|
| Nova Users | Users with access | WHERE NovaAccess != None |
| Activated Users | Hit 3+ conversations | Nova Users with 3+ sessions |
| Activation Rate | % activated | Activated / Nova Users * 100 |

**Engagement Metrics (2 metrics)**:
| Metric | Description | Calculation |
|--------|-------------|-------------|
| Active (7 days) | Active in last week | Users with session in last 7 days |
| Active Rate | % active | Active / Nova Users * 100 |

**Usage Metrics (4 metrics)**:
| Metric | Description | Calculation |
|--------|-------------|-------------|
| Total Sessions | All conversations | COUNT non-deleted sessions |
| Total Messages | All messages | COUNT messages |
| Avg Sessions/User | Usage depth | Sessions / Nova Users |
| Avg Messages/Session | Session depth | Messages / Sessions |

**Total: 14 metrics**, all calculable from existing data.

---

## 3. Implementation Plan

### Phase 1: Backend - Nova Metrics Query

#### 3.1.1 Create Query and Result Records

**File**: `apps/backend/src/Codewrinkles.Application/Nova/GetNovaAlphaMetrics.cs`

```csharp
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

/// <summary>
/// Query to retrieve Nova Alpha metrics for the admin dashboard.
/// </summary>
public sealed record GetNovaAlphaMetricsQuery : IQuery<GetNovaAlphaMetricsResult>;

/// <summary>
/// Nova Alpha metrics result containing application funnel and user engagement data.
/// </summary>
public sealed record GetNovaAlphaMetricsResult(
    // Application funnel
    int TotalApplications,
    int PendingApplications,
    int AcceptedApplications,
    int WaitlistedApplications,
    int RedeemedCodes,

    // User metrics
    int NovaUsers,
    int ActivatedUsers,
    decimal ActivationRate,

    // Engagement metrics
    int ActiveLast7Days,
    decimal ActiveRate,

    // Usage metrics
    int TotalSessions,
    int TotalMessages,
    decimal AvgSessionsPerUser,
    decimal AvgMessagesPerSession
);
```

#### 3.1.2 Create Query Handler

**File**: `apps/backend/src/Codewrinkles.Application/Nova/GetNovaAlphaMetrics.cs` (same file)

```csharp
public sealed class GetNovaAlphaMetricsQueryHandler
    : IQueryHandler<GetNovaAlphaMetricsQuery, GetNovaAlphaMetricsResult>
{
    private readonly INovaMetricsRepository _metricsRepository;

    public GetNovaAlphaMetricsQueryHandler(INovaMetricsRepository metricsRepository)
    {
        _metricsRepository = metricsRepository;
    }

    public async Task<GetNovaAlphaMetricsResult> HandleAsync(
        GetNovaAlphaMetricsQuery query,
        CancellationToken cancellationToken = default)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(
            SpanNames.Admin.GetNovaAlphaMetrics);

        try
        {
            var metrics = await _metricsRepository.GetAlphaMetricsAsync(cancellationToken);

            activity?.SetSuccess(true);
            return metrics;
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }
}
```

#### 3.1.3 Create Metrics Repository Interface

**File**: `apps/backend/src/Codewrinkles.Application/Common/Interfaces/INovaMetricsRepository.cs`

```csharp
namespace Codewrinkles.Application.Common.Interfaces;

using Codewrinkles.Application.Nova;

public interface INovaMetricsRepository
{
    Task<GetNovaAlphaMetricsResult> GetAlphaMetricsAsync(
        CancellationToken cancellationToken = default);
}
```

#### 3.1.4 Create Metrics Repository Implementation

**File**: `apps/backend/src/Codewrinkles.Infrastructure/Persistence/Repositories/Nova/NovaMetricsRepository.cs`

```csharp
namespace Codewrinkles.Infrastructure.Persistence.Repositories.Nova;

using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Application.Nova;
using Codewrinkles.Domain.Identity;
using Codewrinkles.Domain.Nova;
using Microsoft.EntityFrameworkCore;

public sealed class NovaMetricsRepository : INovaMetricsRepository
{
    private readonly ApplicationDbContext _context;

    public NovaMetricsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetNovaAlphaMetricsResult> GetAlphaMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        // ============================================
        // APPLICATION FUNNEL
        // ============================================
        var applications = _context.Set<AlphaApplication>();

        var totalApplications = await applications.CountAsync(cancellationToken);
        var pendingApplications = await applications
            .CountAsync(a => a.Status == AlphaApplicationStatus.Pending, cancellationToken);
        var acceptedApplications = await applications
            .CountAsync(a => a.Status == AlphaApplicationStatus.Accepted, cancellationToken);
        var waitlistedApplications = await applications
            .CountAsync(a => a.Status == AlphaApplicationStatus.Waitlisted, cancellationToken);
        var redeemedCodes = await applications
            .CountAsync(a => a.InviteCodeRedeemed, cancellationToken);

        // ============================================
        // USER METRICS
        // ============================================
        var profiles = _context.Set<Profile>();
        var sessions = _context.Set<ConversationSession>();
        var messages = _context.Set<Message>();

        // Nova users: profiles with NovaAccess != None
        var novaUsers = await profiles
            .CountAsync(p => p.NovaAccess != NovaAccessLevel.None, cancellationToken);

        // Activated users: Nova users with 3+ non-deleted sessions
        var activatedUsers = 0;
        if (novaUsers > 0)
        {
            // Get profile IDs with Nova access
            var novaProfileIds = await profiles
                .Where(p => p.NovaAccess != NovaAccessLevel.None)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            // Count those with 3+ sessions
            activatedUsers = await sessions
                .Where(s => !s.IsDeleted && novaProfileIds.Contains(s.ProfileId))
                .GroupBy(s => s.ProfileId)
                .CountAsync(g => g.Count() >= 3, cancellationToken);
        }

        // Activation rate
        var activationRate = novaUsers > 0
            ? Math.Round((decimal)activatedUsers / novaUsers * 100, 1)
            : 0m;

        // ============================================
        // ENGAGEMENT METRICS
        // ============================================
        var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);

        // Active in last 7 days: Nova users with session activity
        var activeLast7Days = 0;
        if (novaUsers > 0)
        {
            var novaProfileIds = await profiles
                .Where(p => p.NovaAccess != NovaAccessLevel.None)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            activeLast7Days = await sessions
                .Where(s => !s.IsDeleted &&
                           s.LastMessageAt >= sevenDaysAgo &&
                           novaProfileIds.Contains(s.ProfileId))
                .Select(s => s.ProfileId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        // Active rate
        var activeRate = novaUsers > 0
            ? Math.Round((decimal)activeLast7Days / novaUsers * 100, 1)
            : 0m;

        // ============================================
        // USAGE METRICS
        // ============================================
        var totalSessions = await sessions
            .CountAsync(s => !s.IsDeleted, cancellationToken);

        var totalMessages = await messages.CountAsync(cancellationToken);

        var avgSessionsPerUser = novaUsers > 0
            ? Math.Round((decimal)totalSessions / novaUsers, 1)
            : 0m;

        var avgMessagesPerSession = totalSessions > 0
            ? Math.Round((decimal)totalMessages / totalSessions, 1)
            : 0m;

        // ============================================
        // RETURN RESULT
        // ============================================
        return new GetNovaAlphaMetricsResult(
            TotalApplications: totalApplications,
            PendingApplications: pendingApplications,
            AcceptedApplications: acceptedApplications,
            WaitlistedApplications: waitlistedApplications,
            RedeemedCodes: redeemedCodes,
            NovaUsers: novaUsers,
            ActivatedUsers: activatedUsers,
            ActivationRate: activationRate,
            ActiveLast7Days: activeLast7Days,
            ActiveRate: activeRate,
            TotalSessions: totalSessions,
            TotalMessages: totalMessages,
            AvgSessionsPerUser: avgSessionsPerUser,
            AvgMessagesPerSession: avgMessagesPerSession
        );
    }
}
```

#### 3.1.5 Register Repository in DI

**File**: `apps/backend/src/Codewrinkles.Infrastructure/DependencyInjection.cs`

Add to `AddInfrastructure` method:
```csharp
services.AddScoped<INovaMetricsRepository, NovaMetricsRepository>();
```

#### 3.1.6 Add Span Name Constant

**File**: `apps/backend/src/Codewrinkles.Telemetry/SpanNames.cs`

Add to `Admin` class:
```csharp
public const string GetNovaAlphaMetrics = "admin.get_nova_alpha_metrics";
```

#### 3.1.7 Create API Endpoint

**File**: `apps/backend/src/Codewrinkles.API/Modules/Admin/AdminEndpoints.cs`

Add to `MapAdminEndpoints`:
```csharp
group.MapGet("nova/metrics", GetNovaAlphaMetrics)
    .WithName("GetNovaAlphaMetrics")
    .RequireAuthorization("AdminOnly");
```

Add endpoint method:
```csharp
private static async Task<IResult> GetNovaAlphaMetrics(
    [FromServices] IMediator mediator,
    CancellationToken cancellationToken)
{
    var query = new GetNovaAlphaMetricsQuery();
    var result = await mediator.QueryAsync(query, cancellationToken);

    return Results.Ok(result);
}
```

---

### Phase 2: Frontend - Dropdown Navigation

#### 3.2.1 Update AdminNavigation Component

**File**: `apps/frontend/src/features/admin/AdminNavigation.tsx`

Changes needed:
1. Add state for Nova dropdown expanded/collapsed
2. Create expandable nav item with children
3. Update routes: `/admin/alpha` → `/admin/nova/submissions`
4. Add new route: `/admin/nova/metrics`
5. Highlight parent "Nova" when any child is active

**Navigation structure:**
```typescript
// Nav item type with optional children
interface NavItem {
  label: string;
  icon: string;
  path?: string;
  children?: { label: string; icon: string; path: string }[];
}

const navItems: NavItem[] = [
  { label: "Dashboard", icon: "📊", path: "/admin" },
  {
    label: "Nova",
    icon: "🤖",
    children: [
      { label: "Submissions", icon: "📋", path: "/admin/nova/submissions" },
      { label: "Metrics", icon: "📈", path: "/admin/nova/metrics" },
    ]
  },
  { label: "Users", icon: "👥", path: "/admin/users" },
  { label: "Content", icon: "📝", path: "/admin/content" },
  { label: "Settings", icon: "⚙️", path: "/admin/settings" },
];
```

**Dropdown behavior:**
- Click on "Nova" toggles dropdown open/closed
- Dropdown auto-opens if current route is a child
- Child items indented with `pl-4`
- Chevron icon rotates when expanded

#### 3.2.2 Create NovaMetricsPage Component

**File**: `apps/frontend/src/features/admin/NovaMetricsPage.tsx`

**TypeScript interface:**
```typescript
interface NovaAlphaMetrics {
  // Application funnel
  totalApplications: number;
  pendingApplications: number;
  acceptedApplications: number;
  waitlistedApplications: number;
  redeemedCodes: number;

  // User metrics
  novaUsers: number;
  activatedUsers: number;
  activationRate: number;

  // Engagement metrics
  activeLast7Days: number;
  activeRate: number;

  // Usage metrics
  totalSessions: number;
  totalMessages: number;
  avgSessionsPerUser: number;
  avgMessagesPerSession: number;
}
```

**Layout:**
```
┌─────────────────────────────────────────────────────────────────┐
│  Nova Alpha Metrics                                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  APPLICATION FUNNEL                                             │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐        │
│  │ Total  │ │Pending │ │Accepted│ │Waitlist│ │Redeemed│        │
│  │   18   │ │    3   │ │   12   │ │    3   │ │   10   │        │
│  └────────┘ └────────┘ └────────┘ └────────┘ └────────┘        │
│                                                                 │
│  USER METRICS                                                   │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                        │
│  │Nova Users│ │ Activated│ │Activation│                        │
│  │    10    │ │     6    │ │   60%    │                        │
│  └──────────┘ └──────────┘ └──────────┘                        │
│                                                                 │
│  ENGAGEMENT (Last 7 Days)                                       │
│  ┌──────────┐ ┌──────────┐                                     │
│  │  Active  │ │Active    │                                     │
│  │    8     │ │Rate: 80% │                                     │
│  └──────────┘ └──────────┘                                     │
│                                                                 │
│  USAGE                                                          │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐           │
│  │ Sessions │ │ Messages │ │Avg Sess/ │ │Avg Msgs/ │           │
│  │    42    │ │   189    │ │  User    │ │ Session  │           │
│  │          │ │          │ │   4.2    │ │   4.5    │           │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Component pattern** (same as existing DashboardPage):
```typescript
export function NovaMetricsPage(): JSX.Element {
  const [metrics, setMetrics] = useState<NovaAlphaMetrics | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchMetrics = async (): Promise<void> => {
      try {
        const token = localStorage.getItem(config.auth.accessTokenKey);
        const response = await fetch(
          `${config.api.baseUrl}/api/admin/nova/metrics`,
          { headers: { Authorization: `Bearer ${token}` } }
        );

        if (!response.ok) {
          throw new Error("Failed to load metrics");
        }

        const data = await response.json();
        setMetrics(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load metrics");
      } finally {
        setIsLoading(false);
      }
    };

    void fetchMetrics();
  }, []);

  // Render loading, error, or metrics cards...
}
```

#### 3.2.3 Update Routes in App.tsx

**File**: `apps/frontend/src/App.tsx`

```typescript
// Current:
<Route path="alpha" element={<AlphaApplicationsPage />} />

// Change to:
<Route path="nova">
  <Route path="submissions" element={<AlphaApplicationsPage />} />
  <Route path="metrics" element={<NovaMetricsPage />} />
  <Route index element={<Navigate to="submissions" replace />} />
</Route>

// Add redirect for old route:
<Route path="alpha" element={<Navigate to="/admin/nova/submissions" replace />} />
```

---

## 4. File Changes Summary

### New Files

| File | Layer | Purpose |
|------|-------|---------|
| `Application/Nova/GetNovaAlphaMetrics.cs` | Application | Query + Handler |
| `Application/Common/Interfaces/INovaMetricsRepository.cs` | Application | Repository interface |
| `Infrastructure/Persistence/Repositories/Nova/NovaMetricsRepository.cs` | Infrastructure | Repository implementation |
| `frontend/src/features/admin/NovaMetricsPage.tsx` | Frontend | Metrics dashboard page |

### Modified Files

| File | Layer | Changes |
|------|-------|---------|
| `Infrastructure/DependencyInjection.cs` | Infrastructure | Register INovaMetricsRepository |
| `API/Modules/Admin/AdminEndpoints.cs` | API | Add nova/metrics endpoint |
| `Telemetry/SpanNames.cs` | Telemetry | Add span name constant |
| `frontend/src/features/admin/AdminNavigation.tsx` | Frontend | Add dropdown navigation |
| `frontend/src/App.tsx` | Frontend | Update admin routes |

---

## 5. Testing Checklist

### Backend

- [ ] `GetNovaAlphaMetricsQuery` returns all 14 metrics
- [ ] Application counts match database
- [ ] Activated users = Nova users with 3+ sessions
- [ ] Active last 7 days only counts Nova users
- [ ] Averages handle zero division
- [ ] Endpoint requires admin authorization
- [ ] Telemetry span is created

### Frontend

- [ ] Dropdown expands/collapses on click
- [ ] Dropdown auto-opens when child route active
- [ ] Child items have proper indentation
- [ ] Metrics page loads all 14 metrics
- [ ] Loading state while fetching
- [ ] Error state on API failure
- [ ] `/admin/alpha` redirects to `/admin/nova/submissions`
- [ ] Mobile navigation works with dropdown

---

## 6. Implementation Order

1. **Backend** (test via Scalar)
   - Create repository interface
   - Create repository implementation
   - Register in DI
   - Create query + handler
   - Add API endpoint
   - Add span name

2. **Frontend**
   - Update AdminNavigation with dropdown
   - Update App.tsx routes
   - Create NovaMetricsPage
   - Test full flow

---

## 7. Research Sources

- [Instabug - Beta Testing Metrics You Need to Track](https://www.instabug.com/blog/beta-test-metrics)
- [Userpilot - 6 Key Activation Metrics for SaaS](https://userpilot.com/blog/activation-metrics-saas/)
- [ProductLed - Product-Led Growth Metrics](https://productled.com/blog/product-led-growth-metrics)
- [Amplitude - What is Activation Rate](https://amplitude.com/explore/digital-analytics/what-is-activation-rate)
- [Lenny's Newsletter - How to Determine Your Activation Metric](https://www.lennysnewsletter.com/p/how-to-determine-your-activation)

---

**Plan Status**: Implemented (December 20, 2025)
