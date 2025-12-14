# Email Types

```mermaid
flowchart LR
    subgraph Transactional["Transactional Emails"]
        W[Welcome Email]
    end

    subgraph ReEngagement["Re-engagement Emails (24-48h)"]
        N[Notification Reminder]
        F[Feed Update]
    end

    subgraph Winback["Winback Emails"]
        W7[7-Day Winback]
        W30[30-Day Winback]
    end

    style W fill:#20C1AC,color:#000
    style N fill:#35D6C0,color:#000
    style F fill:#35D6C0,color:#000
    style W7 fill:#2AA89A,color:#000
    style W30 fill:#2AA89A,color:#000
```

## Email Details

### Welcome Email
- **Trigger**: Immediately after registration
- **Subject**: "Welcome to Pulse!"
- **CTA**: "Start Exploring" → `/social`
- **Filter**: None (all new users)

### Notification Reminder
- **Trigger**: Daily job, 24-48h inactive
- **Subject**: "You have X unread notifications on Pulse"
- **CTA**: "See What You Missed" → `/social/notifications`
- **Filter**: Must have unread notifications

### Feed Update
- **Trigger**: Daily job, 24-48h inactive
- **Subject**: "Your feed has X new pulses"
- **CTA**: "See Your Feed" → `/social`
- **Filter**: No notifications, but has new pulses from follows

### 7-Day Winback
- **Trigger**: Daily job, 6-7 days inactive
- **Subject**: "We miss you on Pulse!"
- **CTA**: "Come Back to Pulse" → `/pulse`
- **Filter**: None (all users in window)

### 30-Day Winback
- **Trigger**: Daily job, 29-30 days inactive
- **Subject**: "It's been a while - come back to Pulse!"
- **CTA**: "Rejoin the Conversation" → `/pulse`
- **Filter**: None (all users in window)

## Email Flow per Type

```mermaid
sequenceDiagram
    participant T as Trigger
    participant R as Repository
    participant Q as EmailQueue
    participant S as EmailSender
    participant API as Resend

    T->>R: Get candidates
    R-->>T: List of users
    loop For each user
        T->>Q: Queue email (type, data)
    end

    Note over S,API: Background processing
    loop For each queued email
        Q-->>S: Dequeue email
        S->>S: Build template
        S->>API: Send email
        S->>S: Wait 600ms (rate limit)
    end
```
