# Email Infrastructure

> **Status**: Implemented
> **Provider**: Resend (3,000 emails/month free tier, 2 req/sec rate limit)
> **Processing**: Channel<T> + BackgroundService (non-blocking, rate-limited)

---

## Overview

The email system sends five types of emails:

1. **Welcome email** - Queued immediately after registration, sent in background
2. **Notification reminder email** - For inactive users (24-48h) with unread notifications
3. **Feed update email** - For inactive users (24-48h) without notifications but with new content from follows
4. **7-day winback email** - For users inactive 6-7 days (pure "we miss you" message)
5. **30-day winback email** - For users inactive 29-30 days (stronger re-engagement appeal)

Re-engagement emails are processed daily starting at a configurable hour (default: **4 AM UTC**), with winback emails staggered 30 minutes apart to avoid rate limiting.

---

## Architecture

### Flow Diagram

```
┌─────────────────────┐     ┌──────────────────┐     ┌─────────────────────┐
│  Command Handlers   │────▶│  EmailChannel    │────▶│  EmailSender        │
│  (queue emails)     │     │  (Channel<T>)    │     │  BackgroundService  │
└─────────────────────┘     └──────────────────┘     │  (600ms delay)      │
                                   ▲                 └─────────────────────┘
┌─────────────────────┐            │                          │
│  Reengagement       │────────────┤                          ▼
│  BackgroundService  │            │                 ┌───────────────┐
│  (hour:00)          │            │                 │    Resend     │
├─────────────────────┤            │                 │  (2 req/sec)  │
│  SevenDayWinback    │────────────┤                 └───────────────┘
│  BackgroundService  │            │
│  (hour:30)          │            │
├─────────────────────┤            │
│  ThirtyDayWinback   │────────────┘
│  BackgroundService  │
│  (hour+1:00)        │
└─────────────────────┘
```

### Design Principles

1. **Non-blocking**: Emails are queued via Channel<T>, handlers return immediately
2. **Single sender**: All emails flow through one background service
3. **Rate-limited**: 600ms delay between sends to respect Resend's 2 req/sec limit
4. **Fail-safe**: Email failures are logged but never crash the app
5. **DateTimeOffset**: All timestamps use `DateTimeOffset.UtcNow`

---

## File Structure

```
Application/
└── Common/
    └── Interfaces/
        ├── IEmailQueue.cs              # Queue interface (5 methods)
        └── IReengagementRepository.cs  # Repository + ReengagementCandidate + WinbackCandidate

Infrastructure/
├── Configuration/
│   └── EmailSettings.cs                # Resend configuration
├── Email/
│   ├── QueuedEmail.cs                  # Queue message record
│   ├── EmailChannel.cs                 # Channel<T> wrapper (singleton)
│   ├── EmailTemplates.cs               # HTML templates with branding
│   ├── EmailQueue.cs                   # IEmailQueue implementation
│   ├── ResendEmailSender.cs            # Resend API wrapper
│   ├── EmailSenderBackgroundService.cs # Processes queue continuously
│   ├── ReengagementBackgroundService.cs# Daily job (hour:00) - 24-48h emails
│   ├── SevenDayWinbackBackgroundService.cs  # Daily job (hour:30) - 7-day winback
│   └── ThirtyDayWinbackBackgroundService.cs # Daily job (hour+1:00) - 30-day winback
└── Persistence/
    └── Repositories/
        └── ReengagementRepository.cs   # Inactive user queries
```

---

## Configuration

### appsettings.json (non-sensitive defaults)

| Setting | Default | Description |
|---------|---------|-------------|
| `Email:FromName` | `"Codewrinkles"` | Display name in From field |
| `Email:BaseUrl` | `"https://codewrinkles.com"` | Base URL for email links |
| `Email:ReengagementHourUtc` | `8`* | Hour (0-23) when daily job runs |
| `Email:ReengagementBatchSize` | `100` | Max emails per re-engagement run |

*Currently set to 8 for testing. Change to 4 for production via Azure config.

### Environment Variables (Azure App Service)

| Variable | Description |
|----------|-------------|
| `Email__ApiKey` | Resend API key |
| `Email__FromAddress` | Sender email (e.g., `dan@codewrinkles.com`) |
| `Email__WinbackCampaignEnabled` | `true` = all dormant users, `false` = normal 24-48h window |
| `Email__ReengagementHourUtc` | Override the scheduled hour (e.g., `4` for 4 AM UTC) |

### Local Development (User Secrets)

```bash
cd apps/backend/src/Codewrinkles.API
dotnet user-secrets set "Email:ApiKey" "re_xxxxxxxxxxxx"
dotnet user-secrets set "Email:FromAddress" "dan@codewrinkles.com"
```

---

## Re-engagement Logic

### The 24-48 Hour Window

Instead of tracking "last email sent", a time window naturally prevents duplicate sends:

```
Timeline (hours since last login):
0h ─────── 24h ─────── 48h ─────── 72h

          │ ELIGIBLE │
          └──────────┘
          Users in this window get ONE email

Before 24h: Too soon (might just be sleeping/working)
24h-48h:    Eligible window - send re-engagement email
After 48h:  Window passed - email was already sent yesterday
```

### Email Type Decision Tree

```
User in 24-48h window?
├── NO → Don't send
└── YES → Check what to send:
    ├── Has unread notifications? → NOTIFICATION REMINDER
    │   Subject: "You have 5 unread notifications on Pulse"
    │
    ├── No notifications, has new pulses from follows? → FEED UPDATE
    │   Subject: "Your feed has 12 new pulses"
    │
    └── Neither? → Don't send (nothing valuable to offer)
```

### Query Filters

Users are included only if they:
1. Are active (not suspended)
2. Have logged in at least once (`LastLoginAt` is not null)
3. Last login is within the 24-48h window
4. Have unread notifications OR new pulses from people they follow

Results are prioritized by notification count, then by feed activity.

---

## Win-Back Campaign Mode

When first deploying the email system, there may be dormant users who became inactive before the system existed. The normal 24-48 hour window won't catch them.

**Configuration flag:** `Email:WinbackCampaignEnabled`

| Value | Behavior |
|-------|----------|
| `true` (default) | Send emails to ALL users inactive >24 hours (no upper limit) |
| `false` | Normal 24-48 hour window |

**Deployment process:**
1. Deploy with `WinbackCampaignEnabled = true` (the default)
2. Service runs at scheduled hour, sends win-back emails to all dormant users
3. Verify emails were sent (check logs, Resend dashboard)
4. Set `Email__WinbackCampaignEnabled = false` in Azure App Configuration
5. Normal 24-48 hour window logic resumes

**Re-running:** If you need to run another win-back campaign (e.g., after a long period without the service), set the flag back to `true`.

---

## Winback Emails (7-Day and 30-Day)

### Staggered Schedule

To avoid overwhelming Resend's rate limit, the three re-engagement services run 30 minutes apart:

| Service | Schedule | Window | Email Type |
|---------|----------|--------|------------|
| `ReengagementBackgroundService` | `ReengagementHourUtc:00` | 24-48 hours inactive | Notification/Feed |
| `SevenDayWinbackBackgroundService` | `ReengagementHourUtc:30` | 6-7 days inactive | Winback |
| `ThirtyDayWinbackBackgroundService` | `ReengagementHourUtc+1:00` | 29-30 days inactive | Winback |

Example with default hour (`ReengagementHourUtc = 4`):
- **4:00 AM UTC** → 24-48h re-engagement emails (notification reminder or feed update)
- **4:30 AM UTC** → 7-day winback emails
- **5:00 AM UTC** → 30-day winback emails

### Time Window Approach

Each window is a 24-hour slice that ensures users receive exactly one email per tier:

```
User becomes inactive:

Day 0     Day 1-2      Day 6-7       Day 29-30
  │          │            │              │
  │    24-48h window      │              │
  │    ┌─────┴─────┐      │              │
  │    │Notification│     │              │
  │    │or Feed     │     │              │
  │    └───────────┘      │              │
  │                  6-7 day window      │
  │                  ┌────┴────┐         │
  │                  │ 7-Day   │         │
  │                  │ Winback │         │
  │                  └─────────┘         │
  │                              29-30 day window
  │                              ┌───────┴───────┐
  │                              │   30-Day      │
  │                              │   Winback     │
  │                              └───────────────┘

If user logs in at any point, LastLoginAt resets → exits all windows
```

### Winback vs Re-engagement

| Feature | 24-48h Re-engagement | 7/30-Day Winback |
|---------|---------------------|------------------|
| Content filter | Yes (needs notifications OR feed content) | No (sends to ALL users in window) |
| Email content | Shows counts (notifications/pulses) | Pure "we miss you" messaging |
| Repository method | `GetCandidatesAsync` | `GetWinbackCandidatesAsync` |

---

## Email Templates

All templates use Codewrinkles branding:
- Brand teal: `#20C1AC`
- Brand soft: `#35D6C0`
- Light theme for email client compatibility
- Table-based HTML for consistent rendering
- Mobile-responsive design

### Welcome Email
- Sent immediately after registration
- CTA: "Start Exploring" → `/social`

### Notification Reminder Email
- Sent to users with unread notifications
- Shows bold notification count
- CTA: "See What You Missed" → `/social/notifications`

### Feed Update Email
- Sent to users without notifications but with new feed content
- Shows bold pulse count
- CTA: "See Your Feed" → `/social`

### 7-Day Winback Email
- Sent to users inactive 6-7 days
- Pure "we miss you" messaging (no counts)
- CTA: "Come Back to Pulse" → `/pulse`

### 30-Day Winback Email
- Sent to users inactive 29-30 days
- Stronger re-engagement appeal
- CTA: "Rejoin the Conversation" → `/pulse`

---

## Service Lifetimes

| Service | Lifetime | Reason |
|---------|----------|--------|
| `EmailChannel` | Singleton | Shared in-memory queue |
| `EmailQueue` | Singleton | Stateless, uses singleton channel |
| `ResendEmailSender` | Transient | Uses IResend for HTTP calls |
| `IResend` | Transient | Managed by HttpClientFactory |
| `ReengagementRepository` | Scoped | Uses DbContext |
| `EmailSenderBackgroundService` | Singleton | Hosted service |
| `ReengagementBackgroundService` | Singleton | Hosted service |
| `SevenDayWinbackBackgroundService` | Singleton | Hosted service |
| `ThirtyDayWinbackBackgroundService` | Singleton | Hosted service |

---

## Trade-offs

| Decision | Trade-off | Mitigation |
|----------|-----------|------------|
| In-memory queue | Emails lost on app restart | Acceptable at current scale; add persistence if needed |
| Time windows only | No emails outside defined windows (e.g., day 8-28) | Windows cover key re-engagement moments; gaps are intentional |
| Hardcoded templates | Deployment required to change content | Templates rarely change |
| No unsubscribe link | Legal requirement before scaling | Add before reaching 100+ re-engagement emails |
| No email logging | Harder to debug delivery issues | Add EmailLog entity if debugging needed |

---

## Future Extensions

### Unsubscribe Support
- Add `EmailPreferences` to Profile or separate entity
- Add signed unsubscribe token to email links
- Create `/api/email/unsubscribe` endpoint
- Filter unsubscribed users in re-engagement query

### Email Logging
- Create `EmailLog` entity (Id, ToEmail, Subject, Template, SentAt, Status, Error)
- Log all send attempts for debugging
- Add retry logic for transient failures

### Notification Breakdown
- Enhance notification reminder with breakdown:
  - "3 replies to your pulses"
  - "2 new followers"
  - "1 mention"
- Requires grouping by notification type in query

---

## Rate Limiting

Resend has a rate limit of **2 requests per second** on the free tier. The `EmailSenderBackgroundService` adds a 600ms delay between emails to stay safely under this limit.

For batch operations (like win-back campaigns with many emails), this means:
- 10 emails ≈ 6 seconds
- 100 emails ≈ 60 seconds

---

## Troubleshooting

### Diagnosing email issues via Application Insights

**Find all email-related logs:**
```kql
traces
| where timestamp >= ago(4h)
| where message has "re-engagement" or message has "email" or message has "Email"
| order by timestamp desc
| project timestamp, message, severityLevel
```

**Find exceptions (including rate limit errors):**
```kql
exceptions
| where timestamp >= ago(4h)
| order by timestamp desc
| project timestamp, type, outerMessage, innermostMessage
```

**Find re-engagement and winback job execution details:**
```kql
traces
| where timestamp >= ago(4h)
| where message has "candidates" or message has "Queued" or message has "Win-back" or message has "winback"
| order by timestamp asc
| project timestamp, message
```

### Common Issues

| Symptom | Cause | Solution |
|---------|-------|----------|
| `ResendException: Too many requests` | Sending faster than 2 req/sec | Fixed in code with 600ms delay |
| No "Found X candidates" log | Logging level too high | Set `Codewrinkles.Infrastructure.Email: Information` |
| 0 candidates found | No users in time window OR no notifications/feed content | Check with diagnostic SQL query |
| Emails queued but not sent | Check EmailSenderBackgroundService logs | Look for exceptions in App Insights |

### Diagnostic SQL Query

Find all dormant users and see who would receive emails:
```sql
SELECT
    i.Email, p.Name, i.LastLoginAt,
    (SELECT COUNT(*) FROM [notification].Notifications n
     WHERE n.RecipientId = p.Id AND n.IsRead = 0) AS UnreadNotifications,
    (SELECT COUNT(*) FROM [social].Follows f
     INNER JOIN [pulse].Pulses pu ON f.FollowingId = pu.AuthorId
     WHERE f.FollowerId = p.Id AND pu.CreatedAt > i.LastLoginAt) AS NewPulsesFromFollows
FROM [identity].Identities i
INNER JOIN [identity].Profiles p ON i.Id = p.IdentityId
WHERE i.IsActive = 1 AND i.LastLoginAt IS NOT NULL
  AND i.LastLoginAt < DATEADD(HOUR, -24, SYSDATETIMEOFFSET())
ORDER BY i.LastLoginAt DESC
```

---

*Last updated: 2025-12-14*
