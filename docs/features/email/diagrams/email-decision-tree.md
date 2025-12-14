# Email Type Decision Tree

```mermaid
flowchart TD
    Start([User Last Login]) --> Check24{Inactive<br/>24-48 hours?}

    Check24 -->|Yes| HasNotif{Has unread<br/>notifications?}
    Check24 -->|No| Check7{Inactive<br/>6-7 days?}

    HasNotif -->|Yes| NotifEmail[/Notification Reminder Email/]
    HasNotif -->|No| HasFeed{Has new pulses<br/>from follows?}

    HasFeed -->|Yes| FeedEmail[/Feed Update Email/]
    HasFeed -->|No| NoEmail1([No email sent])

    Check7 -->|Yes| Winback7[/7-Day Winback Email/]
    Check7 -->|No| Check30{Inactive<br/>29-30 days?}

    Check30 -->|Yes| Winback30[/30-Day Winback Email/]
    Check30 -->|No| NoEmail2([No email sent])

    style NotifEmail fill:#20C1AC,color:#000
    style FeedEmail fill:#20C1AC,color:#000
    style Winback7 fill:#35D6C0,color:#000
    style Winback30 fill:#35D6C0,color:#000
    style NoEmail1 fill:#666,color:#fff
    style NoEmail2 fill:#666,color:#fff
```

## Decision Logic

### 24-48 Hour Window (Content-Based)
- **Notification Reminder**: User has unread notifications
- **Feed Update**: User has no notifications but follows posted new pulses
- **No Email**: Neither condition met (nothing valuable to offer)

### 7-Day and 30-Day Windows (Pure Winback)
- Sent to ALL users in the window
- No content filter required
- Pure "we miss you" messaging
