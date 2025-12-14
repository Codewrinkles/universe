# Background Service Schedule

```mermaid
flowchart TB
    subgraph Config["Configuration"]
        Hour["ReengagementHourUtc<br/>(default: 8)"]
    end

    subgraph Schedule["Daily Schedule (Example: Hour = 4)"]
        T1["4:00 AM UTC"]
        T2["4:30 AM UTC"]
        T3["5:00 AM UTC"]
    end

    subgraph Services["Background Services"]
        S1[ReengagementBackgroundService]
        S2[SevenDayWinbackBackgroundService]
        S3[ThirtyDayWinbackBackgroundService]
    end

    Hour --> T1
    Hour --> T2
    Hour --> T3

    T1 --> S1
    T2 --> S2
    T3 --> S3

    S1 -->|"24-48h inactive"| Q1[Queue emails]
    S2 -->|"6-7 days inactive"| Q2[Queue emails]
    S3 -->|"29-30 days inactive"| Q3[Queue emails]
```

## Schedule Pattern

| Service | Time Offset | Example (Hour=4) |
|---------|-------------|------------------|
| ReengagementBackgroundService | `hour:00` | 4:00 AM UTC |
| SevenDayWinbackBackgroundService | `hour:30` | 4:30 AM UTC |
| ThirtyDayWinbackBackgroundService | `hour+1:00` | 5:00 AM UTC |

## Why Staggered?

```mermaid
sequenceDiagram
    participant R as Reengagement
    participant S as 7-Day Winback
    participant T as 30-Day Winback
    participant Q as EmailQueue
    participant ES as EmailSender
    participant API as Resend (2 req/sec)

    Note over R,API: 4:00 AM UTC
    R->>Q: Queue N emails
    Note over ES,API: Processing with 600ms delay

    Note over S,API: 4:30 AM UTC (previous batch likely done)
    S->>Q: Queue N emails

    Note over T,API: 5:00 AM UTC (previous batch likely done)
    T->>Q: Queue N emails
```

The 30-minute gaps ensure:
- Previous batch has time to process
- Resend rate limit (2 req/sec) is respected
- No bursts that could trigger throttling

## Calculation Logic

Each service calculates next run time on startup and after each run:

```mermaid
flowchart TD
    Start([Service Starts]) --> GetNow[Get current UTC time]
    GetNow --> CalcTarget[Calculate target time for today]
    CalcTarget --> Check{Current time >= target?}
    Check -->|Yes| Tomorrow[Schedule for tomorrow]
    Check -->|No| Today[Schedule for today]
    Tomorrow --> Wait[Wait until scheduled time]
    Today --> Wait
    Wait --> Run[Execute job]
    Run --> GetNow
```
