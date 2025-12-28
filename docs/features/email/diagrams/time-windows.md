# Email Time Windows

```mermaid
gantt
    title User Inactivity Timeline and Email Windows
    dateFormat X
    axisFormat Day %s

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
        D6[Day 6-7]
        D29[Day 29-30]
    end

    D0 --> D6
    D6 --> D29

    D6 -.->|"6-7 Day Window"| E1[7-Day Winback]
    D29 -.->|"29-30 Day Window"| E2[30-Day Winback]

    style E1 fill:#20C1AC,color:#000
    style E2 fill:#35D6C0,color:#000
```

## Nova-Aware Content

Each window sends different content based on Nova access:

```mermaid
flowchart TD
    subgraph SevenDay["6-7 Day Window"]
        S7[User in window] --> SN{Has Nova?}
        SN -->|Yes| SN7["We miss you on Nova!"]
        SN -->|No| SC7["We miss you on Codewrinkles!"]
    end

    subgraph ThirtyDay["29-30 Day Window"]
        S30[User in window] --> TN{Has Nova?}
        TN -->|Yes| TN30["Access warning"]
        TN -->|No| TC30["Alpha CTA"]
    end

    style SN7 fill:#8B5CF6,color:#fff
    style TN30 fill:#8B5CF6,color:#fff
    style SC7 fill:#20C1AC,color:#000
    style TC30 fill:#20C1AC,color:#000
```

## Why Windows Work

- **No tracking needed**: Windows naturally prevent duplicates
- **User in window = eligible**: Simple query condition
- **User returns = exits all windows**: LastLoginAt resets, no more emails
- **Job runs daily**: Each user gets exactly one email per tier

## Gap Days (Intentional)

Days 1-5, 8-28 have no emails. This is by design:
- Avoid email fatigue
- Focus on key re-engagement moments (1 week, 1 month)
- Respect user attention

---

*Last updated: 2025-12-28*
