# Nova Personalization Roadmap

> **Vision**: Make Cody feel like a colleague who's worked with you for years - not another generic AI chatbot.

> **Database Policy**: All database changes are made through **EF Core migrations only**. We NEVER execute raw SQL to create, update, or delete tables. The SQL shown in this document is for **illustrative purposes only** - actual implementation uses C# entity classes and EF Core configuration.

## The Problem

Milestone 1 gave us a working AI coach, but it feels like ChatGPT with a different system prompt. Users can get the same experience from any AI assistant.

The differentiator is **deep personalization** - Cody should:
- Remember your journey across sessions
- Know your skill level and adapt explanations
- Understand your goals and guide you toward them
- Reference past conversations naturally
- Feel like a friend who genuinely knows you

## Key Insight: Personalization ≠ RAG

| Approach | What It Does | Example |
|----------|--------------|---------|
| **RAG** | Gives Cody knowledge | "Here's how Clean Architecture works..." |
| **Personalization** | Gives Cody knowledge *about you* | "Since you struggled with DI last week, let me explain Clean Architecture starting there..." |

RAG is about what Cody knows. Personalization is about what Cody knows **about the user**.

---

## The 4-Phase Roadmap

### Overview

| Phase | Name | Duration | Key Deliverable |
|-------|------|----------|-----------------|
| 1 | User Profile | 2-3 weeks | Cody knows who you are |
| 2 | Memory Layer | 3-4 weeks | Cody remembers your journey |
| 3 | Skill Tracking | 3-4 weeks | Cody knows what you know |
| 4 | Adaptive Coaching | 3-4 weeks | Cody teaches YOU specifically |

Each phase builds on the previous. Don't skip ahead.

---

## Phase 1: User Profile + Basic Memory

**Goal**: Cody knows who you are and remembers recent conversations.

### What It Enables

- Cody knows your name, role, experience level, goals
- Cody remembers what you talked about last session
- Explanations match your background (Python dev learning C# vs. total beginner)
- Cody references your tech stack and domain

### User Experience

**Before (Generic):**
```
User: How does dependency injection work?
Cody: Dependency Injection (DI) is a design pattern where...
      [Same explanation for everyone]
```

**After Phase 1:**
```
User: How does dependency injection work?
Cody: Coming from Django, you've probably passed dependencies
      manually or used something like dependency-injector.
      .NET's built-in DI is more opinionated...
      [Tailored to user's background]
```

### Data Model

**Architecture Note**: Codewrinkles has a unified identity system. The global `identity.Profiles` table stores shared info (Name, Handle, Bio, Avatar) across all apps (Pulse, Nova, Runwrinkles). Nova only stores **app-specific learning data** in its own schema.

**Schema Design** (implement as EF Core entity + migration):

```
nova.LearnerProfiles
├── Id (GUID, PK)
├── ProfileId (GUID, FK → identity.Profiles, unique)
│
├── Professional Background
│   ├── CurrentRole (string)           -- "Senior Backend Developer"
│   ├── ExperienceYears (int)
│   ├── PrimaryTechStack (string)      -- "Python, Django, PostgreSQL, Redis"
│   └── CurrentProject (string)        -- "E-commerce platform for B2B"
│
├── Learning Preferences
│   ├── LearningGoals (string)         -- "Transition to .NET, learn Clean Architecture"
│   ├── LearningStyle (string)         -- "hands-on" | "theory-first" | "examples-heavy"
│   └── PreferredPace (string)         -- "deep-dive" | "quick-overview" | "balanced"
│
├── AI-Extracted Insights
│   ├── IdentifiedStrengths (string)   -- "Strong with databases, good debugging instincts"
│   └── IdentifiedStruggles (string)   -- "Async patterns, generic types"
│
└── Metadata
    ├── OnboardingCompletedAt (DateTimeOffset?)
    ├── CreatedAt (DateTimeOffset)
    └── UpdatedAt (DateTimeOffset)
```

**What comes from where:**
| Data | Source | Notes |
|------|--------|-------|
| Name, Handle | `identity.Profiles` | Global, used in Cody's greeting |
| Bio | `identity.Profiles` | Could inform context |
| Role, Experience | `nova.LearnerProfiles` | User-entered via Nova settings |
| Tech Stack | `nova.LearnerProfiles` | User-entered via Nova settings |
| Learning Goals | `nova.LearnerProfiles` | User-entered via Nova settings |
| Strengths/Struggles | `nova.LearnerProfiles` | AI-extracted over time |

### UI Integration

**Design Principle**: No one-time onboarding flow. The learning profile is a settings page that users can access and update at any time.

**Why not onboarding?**
- Onboarding feels like a gate before using the app
- Users don't know what they need until they start using Nova
- Profile information evolves over time
- Should be editable, not a one-time form

#### Nova Sidebar Footer

The existing "Learning Paths" placeholder in the sidebar footer becomes "Your Learning":

```
┌─────────────────────────────────────────┐
│  Your Learning                    [⚙️]  │  ← Gear icon links to /nova/settings
├─────────────────────────────────────────┤
│  ┌─────────────────────────────────┐    │
│  │ 🧑‍💻 Senior Backend Dev • 8 yrs │    │  ← Quick profile summary
│  │ Goal: Clean Architecture        │    │
│  └─────────────────────────────────┘    │
│                                         │
│  [Set up your profile]                  │  ← Shown if profile incomplete
└─────────────────────────────────────────┘
```

**States:**
- **No profile**: Shows prompt "Set up your profile to get personalized help"
- **Profile exists**: Shows role, experience, current goal
- **Always**: Gear icon to access full settings

#### Nova Settings Route

New route `/nova/settings` within Nova's layout (sidebar stays visible):

```
/nova/c/new              → New chat
/nova/c/:id              → Chat conversation
/nova/settings           → Learning profile editor (NEW)
/nova/settings/paths     → Learning paths (FUTURE - Phase 3+)
```

#### Learning Profile Form

```
┌─────────────────────────────────────────────────────────┐
│  Your Learning Profile                                   │
├─────────────────────────────────────────────────────────┤
│  Professional Background                                 │
│  ├── Current Role: [________________________]           │
│  │   (dropdown: Student, Junior, Mid, Senior, Lead...)  │
│  ├── Years of Experience: [___]                         │
│  ├── Primary Tech Stack: [________________________]     │
│  │   (multi-select + free text)                         │
│  └── Current Project: [________________________]        │
│      (optional, helps Cody understand your context)     │
│                                                         │
│  Learning Goals                                         │
│  └── What do you want to learn?                         │
│      [____________________________________________]     │
│      [____________________________________________]     │
│                                                         │
│  How You Learn Best                                     │
│  ├── Learning Style:                                    │
│  │   ( ) Show me code examples first                    │
│  │   ( ) Explain theory, then examples                  │
│  │   ( ) Let me try and fail, then explain              │
│  │                                                      │
│  └── Preferred Pace:                                    │
│      ( ) Quick overview - just the essentials           │
│      ( ) Balanced - context + examples                  │
│      ( ) Deep dive - thorough explanations              │
│                                                         │
│                              [Save Changes]             │
└─────────────────────────────────────────────────────────┘
```

#### Why This Approach?

| Alternative | Why Not |
|-------------|---------|
| Add to global `/settings` | Mixes Nova-specific with ecosystem-wide settings |
| Modal/drawer from sidebar | Limited space, feels disconnected |
| Onboarding wizard | One-time, can't update later, feels like a gate |

**This approach:**
- Keeps Nova self-contained (settings live in `/nova/*`)
- Always accessible via sidebar gear icon
- Sidebar stays visible while editing
- User can update anytime as their goals evolve

### System Prompt Integration

```csharp
private string BuildPersonalizedSystemPrompt(UserProfile profile)
{
    return $"""
        You are Cody, an AI learning coach.

        ## About This User
        - Name: {profile.DisplayName}
        - Role: {profile.CurrentRole} ({profile.ExperienceYears} years experience)
        - Tech Stack: {profile.PrimaryTechStack}
        - Currently Working On: {profile.CurrentProject}
        - Learning Goals: {profile.LearningGoals}
        - Learning Style: {profile.LearningStyle}
        - Known Strengths: {profile.IdentifiedStrengths}
        - Areas to Develop: {profile.IdentifiedStruggles}

        ## How to Help This User
        - Reference their {profile.PrimaryTechStack} background when explaining new concepts
        - Adjust depth based on their {profile.ExperienceYears} years of experience
        - They prefer {profile.LearningStyle} learning - adapt accordingly
        - Keep their goal in mind: {profile.LearningGoals}

        {BaseSystemPrompt}
        """;
}
```

### Conversation Context

Include summary of recent sessions:

```csharp
private string GetRecentContext(Guid profileId)
{
    var recentSessions = GetRecentSessions(profileId, count: 3);

    if (!recentSessions.Any())
        return "This is your first conversation with this user.";

    var summaries = recentSessions.Select(s =>
        $"- {s.CreatedAt:MMM dd}: {s.Title} - {s.Summary}");

    return $"""
        ## Recent Conversations
        {string.Join("\n", summaries)}

        Reference these naturally if relevant, but don't force it.
        """;
}
```

### Success Criteria

- [ ] Users discover and complete learning profile setup organically
- [ ] Cody's responses reference user's background when profile exists
- [ ] Users report Cody "understands their context" (survey)
- [ ] Different users get noticeably different explanations for same question

---

## Phase 2: Memory Extraction Pipeline

**Goal**: Cody remembers your journey and references it naturally.

### What It Enables

- "Remember when we debugged that async issue last week?"
- Cody recalls breakthroughs, struggles, and commitments
- Natural callbacks that feel human, not robotic
- Long-term relationship building

### User Experience

**Before (No Memory):**
```
[Monday]
User: I'm confused about async/await
Cody: [Explains async/await]

[Friday]
User: I'm still struggling with async
Cody: [Explains async/await from scratch, no memory of Monday]
```

**After Phase 2:**
```
[Friday]
User: I'm still struggling with async
Cody: We worked through the basics on Monday - you got the
      Task part but CancellationToken was confusing.
      Let's focus there. What specifically is blocking you?
```

### Memory Types

| Type | Description | Example | Retention |
|------|-------------|---------|-----------|
| **Fact** | Stable information about user | "Works at fintech startup" | Long-term |
| **Breakthrough** | Aha moments, concepts clicked | "Finally understood DI on Jan 15" | Long-term |
| **Struggle** | Recurring difficulties | "Keeps confusing await with .Result" | Until resolved |
| **Preference** | How they like to learn | "Prefers short code snippets" | Long-term |
| **Commitment** | Things they said they'd do | "Will practice LINQ this week" | Short-term |
| **Context** | Project/work details | "Building payment integration" | Medium-term |

### Data Model

**Schema Design** (implement as EF Core entity + migration):

```
nova.Memories
├── Id (GUID, PK)
├── ProfileId (GUID, FK → nova.LearnerProfiles)
│
├── Classification
│   ├── Type (string)                    -- fact, breakthrough, struggle, preference, commitment, context
│   └── Category (string?)               -- "async", "architecture", "career", etc.
│
├── Content
│   ├── Content (string)                 -- Natural language memory
│   └── SourceConversationId (GUID?)     -- Where this was extracted from
│
├── Semantic Search
│   └── Embedding (byte[])               -- OpenAI text-embedding-3-small (1536 dimensions)
│
├── Importance & Lifecycle
│   ├── Importance (int, default 5)      -- 1-10 scale
│   ├── Confidence (decimal, default 0.8)-- How sure are we this is accurate
│   ├── IsResolved (bool, default false) -- For struggles/commitments
│   └── ExpiresAt (DateTimeOffset?)      -- NULL = never expires
│
└── Metadata
    ├── CreatedAt (DateTimeOffset)
    ├── LastReferencedAt (DateTimeOffset?)-- When Cody last used this memory
    └── ReferenceCount (int, default 0)

Indexes:
- (ProfileId, Type)
- (ProfileId, Importance DESC)
```

### Memory Extraction Pipeline

```
┌─────────────────────────────────────────────────────────────┐
│                   CONVERSATION ENDS                          │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              BACKGROUND JOB: EXTRACT MEMORIES                │
│                                                              │
│  Input: Full conversation transcript                         │
│  Model: GPT-4o-mini (fast, cheap, good enough)              │
│                                                              │
│  Prompt:                                                     │
│  "Extract memorable information from this conversation.      │
│   For each memory, provide:                                  │
│   - type: fact|breakthrough|struggle|preference|commitment   │
│   - content: natural language description                    │
│   - importance: 1-10                                         │
│   - category: topic area                                     │
│                                                              │
│   Only extract genuinely useful memories, not every detail." │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    PROCESS EXTRACTIONS                       │
│                                                              │
│  For each extracted memory:                                  │
│  1. Check for duplicates (semantic similarity > 0.9)         │
│  2. If duplicate: update existing, bump importance           │
│  3. If new: generate embedding, store                        │
│  4. If contradicts existing: flag for review or update       │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              UPDATE USER PROFILE (if applicable)             │
│                                                              │
│  - New strengths identified → update IdentifiedStrengths     │
│  - New struggles identified → update IdentifiedStruggles     │
│  - Preferences learned → update LearningStyle/PreferredPace  │
└─────────────────────────────────────────────────────────────┘
```

### Memory Retrieval

Before each Cody response:

```csharp
private async Task<string> GetRelevantMemories(Guid profileId, string userMessage)
{
    // 1. Always include high-importance memories (core facts)
    var coreMemories = await GetCoreMemories(profileId, limit: 5);

    // 2. Semantic search for relevant memories
    var embedding = await GenerateEmbedding(userMessage);
    var relevantMemories = await SemanticSearch(profileId, embedding, limit: 5);

    // 3. Recent memories (last 7 days)
    var recentMemories = await GetRecentMemories(profileId, days: 7, limit: 3);

    // Deduplicate and format
    var allMemories = coreMemories
        .Union(relevantMemories)
        .Union(recentMemories)
        .DistinctBy(m => m.Id)
        .OrderByDescending(m => m.Importance)
        .Take(10);

    return FormatMemoriesForPrompt(allMemories);
}
```

### Memory Prompt Section

```
## What You Remember About This User

### Core Facts
- Works as Senior Developer at FinTech startup (high confidence)
- Building a payment processing system (mentioned 3 times)
- Has 5 years Python experience, 6 months into C# transition

### Recent Journey
- Jan 15: Had breakthrough understanding async/await basics
- Jan 18: Struggled with CancellationToken, needs more practice
- Jan 18: Committed to practicing LINQ queries this week

### Patterns You've Noticed
- Learns best from code examples, then theory
- Often rushes ahead before fully understanding fundamentals
- Gets frustrated when explanations are too abstract

Reference these naturally when relevant. Don't force references.
```

### Success Criteria

- [ ] Cody references past conversations accurately
- [ ] Users surprised by what Cody remembers (positive feedback)
- [ ] Memory retrieval adds < 500ms latency
- [ ] False memories rate < 5% (memories that didn't happen)

---

## Phase 3: Skill Tracking

**Goal**: Cody knows what you know and identifies gaps.

### What It Enables

- Cody doesn't over-explain things you've mastered
- Gap identification: "You understand LINQ but haven't touched Expression Trees"
- Learning path suggestions based on actual skill state
- Progress tracking and celebration

### User Experience

**Before (No Skill Awareness):**
```
User: How do I query a database?
Cody: [Explains from basics: what's a database, what's SQL,
       what's LINQ, how to write queries...]
```

**After Phase 3:**
```
User: How do I query a database?
Cody: You're solid with basic LINQ from last week. For your
      payment system, you'll want to look at:
      - Eager loading (Include) since you're joining tables
      - Projection (Select) to avoid over-fetching
      Want to start with eager loading?
```

### Skill Taxonomy

Hierarchical structure of concepts:

```
software-development/
├── languages/
│   ├── csharp/
│   │   ├── basics (syntax, types, control flow)
│   │   ├── oop (classes, inheritance, interfaces)
│   │   ├── linq (basics, advanced, expression trees)
│   │   ├── async (tasks, async-await, cancellation)
│   │   └── advanced (generics, reflection, source generators)
│   └── typescript/
│       └── ...
├── architecture/
│   ├── patterns/
│   │   ├── solid-principles
│   │   ├── design-patterns (creational, structural, behavioral)
│   │   └── architectural-patterns (mvc, cqrs, event-sourcing)
│   ├── clean-architecture/
│   │   ├── layers (domain, application, infrastructure)
│   │   ├── dependency-rule
│   │   └── use-cases
│   └── ddd/
│       ├── tactical (entities, value-objects, aggregates)
│       └── strategic (bounded-contexts, context-mapping)
├── databases/
│   ├── relational/
│   │   ├── sql-fundamentals
│   │   ├── ef-core (basics, migrations, advanced)
│   │   └── performance (indexing, query-optimization)
│   └── ...
└── ...
```

### Data Model

**Schema Design** (implement as EF Core entities + migrations):

```
nova.Concepts (skill taxonomy)
├── Id (string, PK)                      -- "csharp.linq.basics"
├── ParentId (string?, FK → self)        -- "csharp.linq"
├── Name (string)                        -- "LINQ Basics"
├── Description (string?)
├── Difficulty (int, default 1)          -- 1-5 scale
└── EstimatedHours (decimal?)            -- Time to learn

nova.ConceptPrerequisites (many-to-many)
├── ConceptId (string, PK, FK → Concepts)
├── PrerequisiteId (string, PK, FK → Concepts)
└── Strength (decimal, default 1.0)      -- 0.5 = helpful, 1.0 = required

nova.UserSkillStates
├── ProfileId (GUID, PK, FK → LearnerProfiles)
├── ConceptId (string, PK, FK → Concepts)
│
├── Bayesian Knowledge Tracing
│   └── MasteryProbability (decimal)     -- P(learned), 0.0 to 1.0
│
├── Evidence
│   ├── ExposureCount (int)              -- Times topic came up
│   ├── DemonstratedCount (int)          -- Times user showed understanding
│   └── StruggledCount (int)             -- Times user was confused
│
├── Timing
│   ├── FirstExposure (DateTimeOffset?)
│   ├── LastPracticed (DateTimeOffset?)
│   └── NextReviewDue (DateTimeOffset?)  -- Spaced repetition
│
└── UpdatedAt (DateTimeOffset)
```

### Skill Assessment Signals

| Signal | Strength | Example |
|--------|----------|---------|
| **Explicit correct answer** | Strong positive | User correctly explains a concept |
| **Applied in code** | Strong positive | User writes working code using concept |
| **Asked basic question** | Weak negative | "What is dependency injection?" |
| **Asked advanced question** | Positive | "How do I handle circular dependencies in DI?" |
| **Expressed confusion** | Negative | "I don't understand why we need interfaces" |
| **Cody had to re-explain** | Negative | Multiple explanations needed |
| **User taught Cody** | Strong positive | User corrects or expands on Cody's answer |

### Skill Update Logic

```csharp
public void UpdateSkillState(
    UserSkillState state,
    SkillSignal signal)
{
    // Simple Bayesian update
    var prior = state.MasteryProbability;

    var likelihood = signal.Type switch
    {
        SignalType.DemonstratedUnderstanding => 0.9,
        SignalType.AppliedSuccessfully => 0.95,
        SignalType.AskedBasicQuestion => 0.3,
        SignalType.AskedAdvancedQuestion => 0.7,
        SignalType.ExpressedConfusion => 0.2,
        SignalType.NeededReExplanation => 0.25,
        _ => 0.5
    };

    // Bayesian update: P(mastery|evidence) ∝ P(evidence|mastery) * P(mastery)
    var posterior = (likelihood * prior) /
        ((likelihood * prior) + ((1 - likelihood) * (1 - prior)));

    state.MasteryProbability = Math.Clamp(posterior, 0.01, 0.99);
    state.LastPracticed = DateTimeOffset.UtcNow;

    // Update counts
    if (signal.IsPositive)
        state.DemonstratedCount++;
    else
        state.StruggledCount++;

    state.ExposureCount++;
}
```

### Skill-Aware Prompting

```
## User's Skill State (Relevant to This Conversation)

### Mastered (>80% confidence)
- C# Basics (95%) - last practiced 2 days ago
- OOP Fundamentals (88%) - last practiced 1 week ago
- SQL Basics (82%) - last practiced 3 days ago

### Learning (30-80%)
- LINQ Basics (65%) - practicing actively
- Dependency Injection (45%) - some confusion remains
- EF Core Basics (55%) - applied in recent project

### Not Yet Covered (<30%)
- Async/Await (15%) - mentioned but not explored
- Clean Architecture (10%) - expressed interest
- CQRS (0%) - prerequisite: Clean Architecture

### Gaps to Address
- User knows LINQ but hasn't seen GroupBy/Join (gap)
- User understands DI concept but struggles with scopes (struggle)

Adjust your explanations based on these skill levels.
Don't over-explain mastered topics.
Build on what they know when introducing new concepts.
```

### Success Criteria

- [ ] Skill assessments correlate with user self-assessment (>70% agreement)
- [ ] Cody's explanation depth matches skill level
- [ ] Users progress through skill tree measurably
- [ ] Gap identification leads to targeted learning

---

## Phase 4: Adaptive Coaching

**Goal**: Cody teaches YOU specifically, not generic content.

### What It Enables

- Dynamic difficulty adjustment
- Spaced repetition (resurface concepts at optimal times)
- Proactive learning suggestions
- Progress celebration and motivation
- Learning path generation

### Adaptive Behaviors

```
┌─────────────────────────────────────────────────────────────┐
│                    SKILL CONFIDENCE                          │
├───────────┬───────────────┬───────────────┬─────────────────┤
│   < 30%   │   30% - 70%   │   70% - 90%   │     > 90%       │
│  Novice   │   Learning    │  Proficient   │    Mastered     │
├───────────┼───────────────┼───────────────┼─────────────────┤
│ Detailed  │ Moderate      │ Brief         │ Skip or         │
│ scaffolded│ depth with    │ reminders,    │ reference only  │
│ guidance  │ check-ins     │ edge cases    │                 │
├───────────┼───────────────┼───────────────┼─────────────────┤
│ "Let's    │ "You know     │ "Quick note   │ "You've got     │
│ start     │ the basics,   │ on this -     │ this. The key   │
│ with..."  │ let's dig     │ watch for..." │ thing is..."    │
│           │ into..."      │               │                 │
└───────────┴───────────────┴───────────────┴─────────────────┘
```

### Spaced Repetition

Resurface concepts before user forgets them:

```csharp
public DateTimeOffset CalculateNextReview(
    UserSkillState state,
    bool wasSuccessful)
{
    // FSRS-inspired algorithm (simplified)
    var stability = state.MasteryProbability * 30; // Days until 90% retrievability

    if (wasSuccessful)
    {
        // Good recall - increase interval
        stability *= 2.5;
    }
    else
    {
        // Poor recall - decrease interval
        stability *= 0.5;
    }

    stability = Math.Clamp(stability, 1, 365); // 1 day to 1 year

    return DateTimeOffset.UtcNow.AddDays(stability);
}
```

### Proactive Suggestions

Cody initiates based on:

| Trigger | Suggestion |
|---------|------------|
| Skill mastered | "You've nailed LINQ basics! Ready for GroupBy and Join?" |
| Concept due for review | "Quick check - still clear on how async/await works?" |
| Goal alignment | "For your Clean Architecture goal, DI is a good next step" |
| Pattern detected | "You keep hitting null reference issues - want to explore nullable reference types?" |
| Milestone reached | "That's 5 concepts mastered this month! Here's your progress..." |

### Learning Path Generation

Based on:
1. User's stated goals
2. Current skill state
3. Prerequisite graph
4. Optimal learning sequence

```csharp
public LearningPath GeneratePath(
    UserProfile profile,
    string goalConceptId)
{
    // Get all prerequisites (transitive)
    var allPrereqs = GetAllPrerequisites(goalConceptId);

    // Filter to unmastered
    var toLearn = allPrereqs
        .Where(c => GetSkillState(profile.Id, c.Id).MasteryProbability < 0.8)
        .ToList();

    // Topological sort (prerequisites first)
    var ordered = TopologicalSort(toLearn);

    // Group into milestones
    var milestones = GroupIntoMilestones(ordered, maxPerMilestone: 5);

    return new LearningPath
    {
        Goal = goalConceptId,
        Milestones = milestones,
        EstimatedHours = milestones.Sum(m => m.EstimatedHours),
        Prerequisites = toLearn.Count
    };
}
```

### Success Criteria

- [ ] Users complete learning paths at higher rates than self-directed
- [ ] Spaced repetition improves long-term retention (measured by assessments)
- [ ] Proactive suggestions have >50% engagement rate
- [ ] Users report feeling "guided" not "lectured"

---

## Technical Architecture

### Context Assembly Flow

```
┌─────────────────────────────────────────────────────────────┐
│                      USER MESSAGE                            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   CONTEXT ASSEMBLY                           │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ User Profile │  │Skill State   │  │  Memories    │      │
│  │  (always)    │  │ (relevant)   │  │  (semantic)  │      │
│  │   ~500 tok   │  │  ~300 tok    │  │  ~400 tok    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                              │
│  ┌──────────────┐  ┌──────────────┐                        │
│  │ Recent Conv  │  │  Learning    │                        │
│  │   Summary    │  │    Path      │                        │
│  │   ~200 tok   │  │  ~200 tok    │                        │
│  └──────────────┘  └──────────────┘                        │
│                                                              │
│  Total Context: ~1600 tokens (+ system prompt + history)    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    LLM RESPONSE                              │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│              POST-RESPONSE PROCESSING (async)                │
│                                                              │
│  - Extract memories                                          │
│  - Update skill signals                                      │
│  - Update conversation summary                               │
│  - Check for spaced repetition triggers                      │
└─────────────────────────────────────────────────────────────┘
```

### Database Schema Overview

All tables are created via **EF Core migrations only**:

```
nova schema
├── LearnerProfiles       (1:1 with identity.Profiles)
├── Memories              (1:many from LearnerProfiles)
├── Concepts              (skill taxonomy, self-referential)
├── ConceptPrerequisites  (many:many prerequisites)
├── UserSkillStates       (many:many LearnerProfiles ↔ Concepts)
├── ConversationSessions  (existing from M1)
├── Messages              (existing from M1)
└── ConversationSummaries (1:1 with ConversationSessions)
```

### Vector Storage Options

| Option | Pros | Cons | Recommendation |
|--------|------|------|----------------|
| **PostgreSQL + pgvector** | Simple, single DB | Separate from SQL Server | If adding Postgres anyway |
| **Azure AI Search** | Managed, scales well | Additional service cost | For production scale |
| **SQL Server + embeddings table** | Keep everything in SQL Server | Manual similarity search | Start here |
| **Qdrant/Pinecone** | Purpose-built, fast | Another service to manage | If scale demands |

**Recommendation**: Start with SQL Server. Store embeddings as VARBINARY, compute cosine similarity in application code. Move to dedicated vector DB when you have >100K memories.

---

## The "Colleague Test"

Use this to evaluate if personalization is working:

| Scenario | Generic AI Response | Colleague-Like Response |
|----------|--------------------|-----------------------|
| User returns after a week | "Hello! How can I help?" | "Hey! How did that payment integration go?" |
| User asks about DI | Full tutorial from scratch | "Like what you did in that auth service, but..." |
| User is stuck | "Here's the solution..." | "You figured out something similar last month with..." |
| User makes progress | "Good job." | "That's 3 concepts this week! Remember when DI confused you?" |
| User asks next steps | Generic learning roadmap | "Based on your Clean Architecture goal, I'd suggest..." |

---

## Implementation Order

### Now: Phase 1 Sprint
1. Create `LearnerProfile` entity + EF Core migration
2. Build Nova settings page (`/nova/settings`) with learning profile form
3. Update sidebar footer with profile summary + gear icon
4. Inject profile into system prompt
5. Add recent conversation summary to context
6. Test with real users

### Next: Phase 2 Sprint
1. Design memory extraction prompt
2. Create `Memory` entity + EF Core migration
3. Build extraction background job
4. Implement semantic retrieval
5. Test memory quality

### Later: Phase 3-4
- Build skill taxonomy
- Implement skill tracking
- Add adaptive behaviors
- Learning path generation

---

## Success Metrics

### Engagement
- Return rate (users who come back within 7 days)
- Session frequency and duration
- Messages per session

### Personalization Quality
- "Cody knows me" survey score (1-10)
- Memory accuracy (spot checks)
- Explanation appropriateness (user feedback)

### Learning Outcomes
- Skill progression velocity
- Goal completion rate
- Long-term retention (spaced repetition effectiveness)

### Technical
- Context assembly latency (< 500ms)
- Memory extraction accuracy (> 90%)
- Skill assessment correlation with self-assessment (> 70%)

---

## References

### Memory Architectures
- [MemGPT](https://memgpt.readme.io/) - Hierarchical memory for LLMs
- [Mem0](https://mem0.ai/) - Production memory layer
- [LangChain Memory](https://python.langchain.com/docs/modules/memory/) - Memory abstractions

### Knowledge Tracing
- [Bayesian Knowledge Tracing](https://en.wikipedia.org/wiki/Bayesian_knowledge_tracing)
- [Deep Knowledge Tracing](https://stanford.edu/~cpiech/bio/papers/deepKnowledgeTracing.pdf)
- [FSRS Spaced Repetition](https://github.com/open-spaced-repetition/fsrs4anki)

### Adaptive Learning Products
- [Duolingo's Birdbrain](https://blog.duolingo.com/how-we-learn-how-you-learn/)
- [Khan Academy's Khanmigo](https://www.khanmigo.ai/)
- [Squirrel AI](https://squirrelai.com/)
