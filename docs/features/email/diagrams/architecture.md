# Email System Architecture

```mermaid
flowchart TB
    subgraph Producers["Email Producers"]
        CH[Command Handlers]
        RE[ReengagementBackgroundService<br/>hour:00]
        SW[SevenDayWinbackBackgroundService<br/>hour:30]
        TW[ThirtyDayWinbackBackgroundService<br/>hour+1:00]
    end

    subgraph Queue["In-Memory Queue"]
        EC[EmailChannel<br/>Channel&lt;T&gt;]
    end

    subgraph Consumer["Email Consumer"]
        ES[EmailSenderBackgroundService<br/>600ms delay between sends]
    end

    subgraph External["External Service"]
        RS[Resend API<br/>2 req/sec limit]
    end

    CH -->|queue| EC
    RE -->|queue| EC
    SW -->|queue| EC
    TW -->|queue| EC
    EC --> ES
    ES -->|send| RS
```

## Description

The email system follows a producer-consumer pattern:

1. **Producers** queue emails to the channel:
   - Command handlers (welcome emails after registration)
   - Background services (re-engagement and winback emails)

2. **Queue** (EmailChannel) is a thread-safe `Channel<T>` that decouples producers from the sending process

3. **Consumer** (EmailSenderBackgroundService) reads from the queue and sends emails with rate limiting

4. **External Service** (Resend) receives the actual email send requests
