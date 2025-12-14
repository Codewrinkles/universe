# Email Time Windows

```mermaid
gantt
    title User Inactivity Timeline and Email Windows
    dateFormat X
    axisFormat Day %s

    section 24-48h
    Notification/Feed Email    :active, 1, 2

    section 7-Day
    7-Day Winback Email       :active, 6, 7

    section 30-Day
    30-Day Winback Email      :active, 29, 30
```

## Window Approach

Each window is a 24-hour slice that ensures users receive exactly one email per tier:

```mermaid
flowchart LR
    subgraph Timeline["Days Since Last Login"]
        D0[Day 0]
        D1[Day 1-2]
        D6[Day 6-7]
        D29[Day 29-30]
    end

    D0 --> D1
    D1 --> D6
    D6 --> D29

    D1 -.->|"24-48h Window"| E1[Notification/Feed]
    D6 -.->|"6-7 Day Window"| E2[7-Day Winback]
    D29 -.->|"29-30 Day Window"| E3[30-Day Winback]

    style E1 fill:#20C1AC,color:#000
    style E2 fill:#35D6C0,color:#000
    style E3 fill:#35D6C0,color:#000
```

## Why Windows Work

- **No tracking needed**: Windows naturally prevent duplicates
- **User in window = eligible**: Simple query condition
- **User returns = exits all windows**: LastLoginAt resets, no more emails
- **Job runs daily**: Each user gets exactly one email per tier

## Gap Days (Intentional)

Days 3-5, 8-28 have no emails. This is by design:
- Avoid email fatigue
- Focus on key re-engagement moments
- Respect user attention
