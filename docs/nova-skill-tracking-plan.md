# Nova Phase 3: Skill Tracking Implementation Plan

> **Goal**: Cody knows what you know and adjusts explanations accordingly.

## Overview

Skill tracking enables Cody to:
- Know your mastery level for each concept
- Adjust explanation depth (skip basics you've mastered)
- Identify knowledge gaps
- Track learning progress over time

---

## 1. Domain Model

### 1.1 Concept Entity (Skill Taxonomy Node)

```csharp
namespace Codewrinkles.Domain.Nova;

public sealed class Concept
{
    public string Id { get; private set; }              // "csharp.linq.basics"
    public string? ParentId { get; private set; }       // "csharp.linq"
    public string Name { get; private set; }            // "LINQ Basics"
    public string? Description { get; private set; }
    public int Difficulty { get; private set; }         // 1-5 scale
    public int DisplayOrder { get; private set; }       // For UI ordering

    // Navigation
    public Concept? Parent { get; private set; }
    public ICollection<Concept> Children { get; private set; }
    public ICollection<ConceptPrerequisite> Prerequisites { get; private set; }
    public ICollection<ConceptPrerequisite> DependentConcepts { get; private set; }
}
```

**Key decisions:**
- `Id` is a string path (e.g., "csharp.linq.basics") for readability and hierarchy
- Self-referential for parent-child relationships
- Difficulty 1-5 helps with learning path ordering

### 1.2 ConceptPrerequisite Entity (Many-to-Many)

```csharp
public sealed class ConceptPrerequisite
{
    public string ConceptId { get; private set; }       // The concept
    public string PrerequisiteId { get; private set; }  // What it requires
    public decimal Strength { get; private set; }       // 0.5 = helpful, 1.0 = required

    // Navigation
    public Concept Concept { get; private set; }
    public Concept Prerequisite { get; private set; }
}
```

**Example:**
- "csharp.async.cancellation" requires "csharp.async.basics" (strength: 1.0)
- "architecture.clean-architecture" is helped by "architecture.solid" (strength: 0.7)

### 1.3 UserSkillState Entity

```csharp
public sealed class UserSkillState
{
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }         // FK to LearnerProfile
    public string ConceptId { get; private set; }       // FK to Concept

    // Bayesian Knowledge Tracing
    public decimal MasteryProbability { get; private set; }  // P(learned), 0.0 to 1.0

    // Evidence counts
    public int ExposureCount { get; private set; }      // Times topic came up
    public int DemonstratedCount { get; private set; }  // Times user showed understanding
    public int StruggledCount { get; private set; }     // Times user was confused

    // Timing
    public DateTimeOffset? FirstExposure { get; private set; }
    public DateTimeOffset? LastPracticed { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public LearnerProfile Profile { get; private set; }
    public Concept Concept { get; private set; }
}
```

**Mastery levels:**
| Probability | Level | Cody's Behavior |
|-------------|-------|-----------------|
| < 0.30 | Novice | Detailed, scaffolded explanations |
| 0.30 - 0.70 | Learning | Moderate depth with check-ins |
| 0.70 - 0.90 | Proficient | Brief reminders, edge cases |
| > 0.90 | Mastered | Skip or reference only |

### 1.4 SkillSignal Value Object

```csharp
public enum SkillSignalType
{
    DemonstratedUnderstanding,  // User correctly explained concept
    AppliedSuccessfully,        // User wrote working code using concept
    AskedBasicQuestion,         // "What is X?"
    AskedAdvancedQuestion,      // "How do I handle edge case Y in X?"
    ExpressedConfusion,         // "I don't understand X"
    NeededReExplanation,        // Cody had to re-explain
    CorrectedCody,              // User taught/corrected Cody
    ReferencedCorrectly         // User referenced concept correctly in context
}

public sealed record SkillSignal(
    string ConceptId,
    SkillSignalType Type,
    decimal Confidence,         // How sure we are about this signal
    string? Evidence            // The text that triggered this signal
);
```

---

## 2. Skill Taxonomy (Seed Data)

The taxonomy is hierarchical. We'll start with core .NET and architecture concepts, then expand.

### 2.1 Initial Taxonomy Structure

```
software-development/
├── languages/
│   ├── csharp/
│   │   ├── basics/
│   │   │   ├── syntax              (variables, control flow, operators)
│   │   │   ├── types               (value types, reference types, nullability)
│   │   │   └── collections         (arrays, lists, dictionaries)
│   │   ├── oop/
│   │   │   ├── classes             (constructors, properties, methods)
│   │   │   ├── inheritance         (virtual, override, abstract)
│   │   │   ├── interfaces          (contracts, multiple inheritance)
│   │   │   └── polymorphism        (runtime dispatch, is/as)
│   │   ├── linq/
│   │   │   ├── basics              (Where, Select, First, Any)
│   │   │   ├── aggregation         (Count, Sum, Average, GroupBy)
│   │   │   ├── joins               (Join, GroupJoin, SelectMany)
│   │   │   └── expression-trees    (IQueryable, deferred execution)
│   │   ├── async/
│   │   │   ├── basics              (Task, async/await, Task.Run)
│   │   │   ├── cancellation        (CancellationToken, cooperative cancellation)
│   │   │   ├── parallelism         (Parallel.ForEach, PLINQ, Task.WhenAll)
│   │   │   └── synchronization     (SemaphoreSlim, lock, concurrent collections)
│   │   └── advanced/
│   │       ├── generics            (type parameters, constraints, variance)
│   │       ├── delegates-events    (Action, Func, event pattern)
│   │       ├── reflection          (Type inspection, dynamic invocation)
│   │       └── source-generators   (compile-time code generation)
│   │
│   └── typescript/
│       ├── basics/
│       │   ├── types               (primitives, unions, intersections)
│       │   ├── interfaces          (structural typing, optional properties)
│       │   └── functions           (arrow functions, overloads)
│       ├── advanced/
│       │   ├── generics            (type parameters, constraints)
│       │   ├── utility-types       (Partial, Pick, Omit, Record)
│       │   └── type-guards         (narrowing, discriminated unions)
│       └── react/
│           ├── components          (functional components, props)
│           ├── hooks               (useState, useEffect, custom hooks)
│           ├── state-management    (context, reducers, external stores)
│           └── performance         (memo, useMemo, useCallback)
│
├── architecture/
│   ├── fundamentals/
│   │   ├── separation-of-concerns  (why layers matter)
│   │   ├── coupling-cohesion       (tight vs loose coupling)
│   │   └── abstraction             (hiding complexity, interfaces)
│   ├── solid/
│   │   ├── single-responsibility   (one reason to change)
│   │   ├── open-closed             (open for extension, closed for modification)
│   │   ├── liskov-substitution     (subtypes must be substitutable)
│   │   ├── interface-segregation   (small, focused interfaces)
│   │   └── dependency-inversion    (depend on abstractions)
│   ├── patterns/
│   │   ├── creational/
│   │   │   ├── factory             (Factory Method, Abstract Factory)
│   │   │   ├── builder             (step-by-step construction)
│   │   │   └── singleton           (single instance, when appropriate)
│   │   ├── structural/
│   │   │   ├── adapter             (interface translation)
│   │   │   ├── decorator           (dynamic behavior addition)
│   │   │   └── facade              (simplified interface)
│   │   └── behavioral/
│   │       ├── strategy            (interchangeable algorithms)
│   │       ├── observer            (event notification)
│   │       └── command             (encapsulated requests)
│   ├── clean-architecture/
│   │   ├── layers                  (domain, application, infrastructure, presentation)
│   │   ├── dependency-rule         (dependencies point inward)
│   │   ├── use-cases               (application services, handlers)
│   │   └── boundaries              (ports and adapters)
│   ├── ddd/
│   │   ├── tactical/
│   │   │   ├── entities            (identity, lifecycle)
│   │   │   ├── value-objects       (immutable, equality by value)
│   │   │   ├── aggregates          (consistency boundaries)
│   │   │   └── repositories        (collection-like persistence)
│   │   └── strategic/
│   │       ├── bounded-contexts    (explicit boundaries)
│   │       ├── ubiquitous-language (shared vocabulary)
│   │       └── context-mapping     (relationships between contexts)
│   └── cqrs/
│       ├── basics                  (command vs query separation)
│       ├── handlers                (command handlers, query handlers)
│       └── event-sourcing          (storing events, projections)
│
├── databases/
│   ├── relational/
│   │   ├── sql-fundamentals        (SELECT, JOIN, GROUP BY)
│   │   ├── schema-design           (normalization, relationships)
│   │   ├── indexing                (clustered, non-clustered, covering)
│   │   └── transactions            (ACID, isolation levels)
│   ├── ef-core/
│   │   ├── basics                  (DbContext, DbSet, entities)
│   │   ├── configuration           (Fluent API, conventions)
│   │   ├── migrations              (code-first schema management)
│   │   ├── querying                (LINQ to entities, tracking)
│   │   └── performance             (eager loading, split queries, compiled queries)
│   └── nosql/
│       ├── document-stores         (MongoDB, CosmosDB concepts)
│       └── key-value               (Redis, caching patterns)
│
├── api-design/
│   ├── rest/
│   │   ├── fundamentals            (resources, HTTP methods, status codes)
│   │   ├── best-practices          (versioning, pagination, filtering)
│   │   └── hateoas                 (hypermedia, discoverability)
│   ├── authentication/
│   │   ├── jwt                     (tokens, claims, validation)
│   │   ├── oauth                   (flows, scopes, refresh tokens)
│   │   └── authorization           (roles, policies, claims-based)
│   └── aspnet-core/
│       ├── minimal-apis            (endpoints, route handlers)
│       ├── middleware              (pipeline, request processing)
│       └── dependency-injection    (service lifetimes, registration)
│
└── testing/
    ├── fundamentals/
    │   ├── unit-testing            (isolation, assertions, AAA pattern)
    │   ├── test-doubles            (mocks, stubs, fakes)
    │   └── test-design             (what to test, coverage)
    ├── integration/
    │   ├── database-tests          (test containers, in-memory)
    │   └── api-tests               (WebApplicationFactory)
    └── tdd/
        ├── red-green-refactor      (the cycle)
        └── test-first-design       (tests drive design)
```

### 2.2 Seed Data Strategy

We'll create a `ConceptSeeder` that runs on application startup (development) or via a migration (production).

**Option A: JSON seed file** (recommended for maintainability)
```json
{
  "concepts": [
    {
      "id": "csharp",
      "name": "C#",
      "difficulty": 1,
      "children": [
        {
          "id": "csharp.basics",
          "name": "C# Basics",
          "difficulty": 1,
          "children": [
            {
              "id": "csharp.basics.syntax",
              "name": "Syntax & Control Flow",
              "description": "Variables, operators, if/else, loops, switch",
              "difficulty": 1
            }
          ]
        }
      ]
    }
  ],
  "prerequisites": [
    { "concept": "csharp.oop.interfaces", "requires": "csharp.oop.classes", "strength": 1.0 },
    { "concept": "csharp.linq.basics", "requires": "csharp.basics.collections", "strength": 1.0 }
  ]
}
```

**Option B: Fluent C# builder** (better compile-time safety)
```csharp
public static class ConceptTaxonomy
{
    public static IEnumerable<Concept> Build() =>
    [
        Concept("csharp", "C#", difficulty: 1)
            .WithChild("basics", "C# Basics", difficulty: 1)
                .WithChild("syntax", "Syntax & Control Flow", difficulty: 1)
                .WithChild("types", "Type System", difficulty: 1)
            .Parent()
            .WithChild("oop", "Object-Oriented Programming", difficulty: 2)
            // ...
    ];
}
```

**Recommendation**: Start with Option A (JSON) for rapid iteration, consider Option B if taxonomy changes frequently and needs type safety.

---

## 3. Skill Signal Extraction

### 3.1 When to Extract Signals

Signals are extracted during memory extraction (same background process). We extend the existing `ExtractMemories` handler.

```
Conversation ends
       │
       ▼
┌─────────────────────────────────────┐
│  ExtractMemoriesAndSignals Handler  │
│                                     │
│  1. Extract memories (existing)     │
│  2. Extract skill signals (new)     │
│  3. Update UserSkillStates          │
└─────────────────────────────────────┘
```

### 3.2 Signal Extraction Prompt

```
Analyze this conversation for evidence of the user's skill levels.

For each skill signal you detect, provide:
- conceptId: The specific concept (from the taxonomy)
- signalType: One of [DemonstratedUnderstanding, AppliedSuccessfully, AskedBasicQuestion, AskedAdvancedQuestion, ExpressedConfusion, NeededReExplanation, CorrectedCody, ReferencedCorrectly]
- confidence: 0.0-1.0 how confident you are in this signal
- evidence: Brief quote or description

Concept taxonomy (partial, relevant to this conversation):
{relevantTaxonomySubset}

Conversation:
{conversationMessages}

Rules:
- Only extract signals for concepts actually discussed
- AskedBasicQuestion = "What is X?" style questions
- AskedAdvancedQuestion = questions about edge cases, advanced usage
- DemonstratedUnderstanding = user correctly explains or uses a concept
- ExpressedConfusion = explicit confusion ("I don't get it", "this is confusing")
- NeededReExplanation = Cody had to explain multiple times
- Be conservative - only report signals you're confident about

Return as JSON array:
[
  {
    "conceptId": "csharp.async.basics",
    "signalType": "AskedBasicQuestion",
    "confidence": 0.9,
    "evidence": "User asked 'What does async do?'"
  }
]
```

### 3.3 Relevant Taxonomy Selection

We don't send the entire taxonomy to the LLM. Instead:

1. **Keyword matching**: Extract keywords from conversation, match to concept names/descriptions
2. **Embedding similarity**: Use semantic search to find relevant concepts
3. **Hierarchical expansion**: Include parents and siblings of matched concepts

```csharp
public async Task<IReadOnlyList<Concept>> GetRelevantConcepts(
    string conversationText,
    int maxConcepts = 30)
{
    // 1. Generate embedding for conversation
    var embedding = await _embeddingService.GenerateEmbeddingAsync(conversationText);

    // 2. Find semantically similar concepts (requires concept embeddings)
    var similar = await _conceptRepository.FindSimilarAsync(embedding, limit: 15);

    // 3. Expand to include parents (for context)
    var expanded = ExpandWithAncestors(similar);

    return expanded.Take(maxConcepts).ToList();
}
```

---

## 4. Bayesian Knowledge Tracing

### 4.1 The Algorithm

Bayesian Knowledge Tracing (BKT) updates mastery probability based on evidence.

```csharp
public sealed class SkillStateUpdater
{
    // Prior probabilities (can be tuned)
    private const decimal PriorMastery = 0.3m;      // P(learned) before any evidence
    private const decimal LearnRate = 0.1m;          // P(transition to learned)
    private const decimal SlipRate = 0.1m;           // P(mistake when learned)
    private const decimal GuessRate = 0.2m;          // P(correct when not learned)

    public decimal UpdateMastery(
        decimal currentMastery,
        SkillSignalType signalType,
        decimal signalConfidence)
    {
        // Likelihood of this signal given mastery
        var likelihoodIfMastered = GetLikelihood(signalType, isMastered: true);
        var likelihoodIfNotMastered = GetLikelihood(signalType, isMastered: false);

        // Apply signal confidence
        likelihoodIfMastered = AdjustForConfidence(likelihoodIfMastered, signalConfidence);
        likelihoodIfNotMastered = AdjustForConfidence(likelihoodIfNotMastered, signalConfidence);

        // Bayes' theorem
        var numerator = likelihoodIfMastered * currentMastery;
        var denominator = numerator + (likelihoodIfNotMastered * (1 - currentMastery));

        var posterior = numerator / denominator;

        // Learning transition: even after negative evidence, there's a chance they learned
        posterior = posterior + (1 - posterior) * LearnRate;

        return Math.Clamp(posterior, 0.01m, 0.99m);
    }

    private decimal GetLikelihood(SkillSignalType signalType, bool isMastered)
    {
        // P(signal | mastery state)
        return (signalType, isMastered) switch
        {
            // Strong positive signals
            (SkillSignalType.DemonstratedUnderstanding, true) => 0.95m,
            (SkillSignalType.DemonstratedUnderstanding, false) => 0.15m,

            (SkillSignalType.AppliedSuccessfully, true) => 0.98m,
            (SkillSignalType.AppliedSuccessfully, false) => 0.10m,

            (SkillSignalType.CorrectedCody, true) => 0.99m,
            (SkillSignalType.CorrectedCody, false) => 0.05m,

            // Positive signals
            (SkillSignalType.AskedAdvancedQuestion, true) => 0.80m,
            (SkillSignalType.AskedAdvancedQuestion, false) => 0.20m,

            (SkillSignalType.ReferencedCorrectly, true) => 0.85m,
            (SkillSignalType.ReferencedCorrectly, false) => 0.25m,

            // Negative signals
            (SkillSignalType.AskedBasicQuestion, true) => 0.20m,
            (SkillSignalType.AskedBasicQuestion, false) => 0.80m,

            (SkillSignalType.ExpressedConfusion, true) => 0.15m,
            (SkillSignalType.ExpressedConfusion, false) => 0.85m,

            (SkillSignalType.NeededReExplanation, true) => 0.25m,
            (SkillSignalType.NeededReExplanation, false) => 0.75m,

            _ => 0.50m
        };
    }
}
```

### 4.2 Handling Multiple Signals

When multiple signals are extracted from a conversation:

```csharp
public async Task ProcessSignals(
    Guid profileId,
    IReadOnlyList<SkillSignal> signals)
{
    // Group signals by concept
    var byConceptId = signals.GroupBy(s => s.ConceptId);

    foreach (var group in byConceptId)
    {
        var state = await GetOrCreateSkillState(profileId, group.Key);

        // Apply signals in order of confidence (highest first)
        foreach (var signal in group.OrderByDescending(s => s.Confidence))
        {
            state.MasteryProbability = _updater.UpdateMastery(
                state.MasteryProbability,
                signal.Type,
                signal.Confidence);

            // Update evidence counts
            if (IsPositiveSignal(signal.Type))
                state.DemonstratedCount++;
            else
                state.StruggledCount++;

            state.ExposureCount++;
        }

        state.LastPracticed = DateTimeOffset.UtcNow;
        state.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

---

## 5. Skill-Aware Prompting

### 5.1 Retrieving Relevant Skills for Prompt

```csharp
public sealed class GetSkillsForContext
{
    public async Task<SkillContext> ExecuteAsync(
        Guid profileId,
        string userMessage)
    {
        // 1. Find concepts relevant to this message
        var relevantConcepts = await GetRelevantConcepts(userMessage);
        var conceptIds = relevantConcepts.Select(c => c.Id).ToList();

        // 2. Get user's skill states for these concepts
        var skillStates = await _repository.GetSkillStatesAsync(profileId, conceptIds);

        // 3. Also get high-mastery skills (might be relevant for bridging)
        var masteredSkills = await _repository.GetTopMasteredAsync(profileId, limit: 5);

        // 4. Identify gaps (prerequisites not mastered)
        var gaps = await IdentifyGaps(profileId, conceptIds);

        return new SkillContext
        {
            RelevantSkills = skillStates,
            MasteredSkills = masteredSkills,
            Gaps = gaps
        };
    }
}
```

### 5.2 System Prompt Injection

Extend `SystemPrompts.BuildPersonalizedPrompt`:

```csharp
public static string BuildPersonalizedPrompt(
    LearnerProfile? profile,
    IReadOnlyList<MemoryDto>? memories = null,
    SkillContext? skills = null)  // NEW
{
    var prompt = new StringBuilder(CodyCoachBase);

    if (profile?.HasUserData() == true)
        AppendProfilePersonalization(prompt, profile);

    if (memories?.Count > 0)
        prompt.Append(BuildMemoryContext(memories));

    if (skills is not null)
        prompt.Append(BuildSkillContext(skills));  // NEW

    return prompt.ToString();
}

private static string BuildSkillContext(SkillContext skills)
{
    var sb = new StringBuilder();
    sb.AppendLine();
    sb.AppendLine("## User's Skill Levels (Relevant to This Conversation)");

    // Group by mastery level
    var mastered = skills.RelevantSkills.Where(s => s.MasteryProbability > 0.85m);
    var proficient = skills.RelevantSkills.Where(s => s.MasteryProbability is > 0.60m and <= 0.85m);
    var learning = skills.RelevantSkills.Where(s => s.MasteryProbability is > 0.30m and <= 0.60m);
    var novice = skills.RelevantSkills.Where(s => s.MasteryProbability <= 0.30m);

    if (mastered.Any())
    {
        sb.AppendLine("Mastered (skip basics, reference only):");
        foreach (var skill in mastered)
            sb.AppendLine($"  - {skill.ConceptName} ({skill.MasteryProbability:P0})");
    }

    if (proficient.Any())
    {
        sb.AppendLine("Proficient (brief explanations, focus on nuances):");
        foreach (var skill in proficient)
            sb.AppendLine($"  - {skill.ConceptName} ({skill.MasteryProbability:P0})");
    }

    if (learning.Any())
    {
        sb.AppendLine("Learning (moderate depth, check understanding):");
        foreach (var skill in learning)
            sb.AppendLine($"  - {skill.ConceptName} ({skill.MasteryProbability:P0})");
    }

    if (novice.Any())
    {
        sb.AppendLine("Novice (detailed explanations, build from fundamentals):");
        foreach (var skill in novice)
            sb.AppendLine($"  - {skill.ConceptName} ({skill.MasteryProbability:P0})");
    }

    if (skills.Gaps.Any())
    {
        sb.AppendLine();
        sb.AppendLine("Knowledge Gaps to Address:");
        foreach (var gap in skills.Gaps)
            sb.AppendLine($"  - {gap.ConceptName}: missing prerequisite {gap.MissingPrerequisite}");
    }

    sb.AppendLine();
    sb.AppendLine("Adjust your explanation depth based on these skill levels.");
    sb.AppendLine("Don't over-explain mastered topics. Build on what they know.");

    return sb.ToString();
}
```

---

## 6. Database Schema

### 6.1 EF Core Entities

**Concept.cs**
```csharp
public sealed class Concept
{
    public string Id { get; private set; }
    public string? ParentId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int Difficulty { get; private set; }
    public int DisplayOrder { get; private set; }
    public ReadOnlyMemory<float>? Embedding { get; private set; }  // For semantic search

    public Concept? Parent { get; private set; }
    public ICollection<Concept> Children { get; private set; } = [];
    public ICollection<ConceptPrerequisite> Prerequisites { get; private set; } = [];
    public ICollection<ConceptPrerequisite> DependentConcepts { get; private set; } = [];

#pragma warning disable CS8618
    private Concept() { }
#pragma warning restore CS8618

    public static Concept Create(
        string id,
        string name,
        string? parentId = null,
        string? description = null,
        int difficulty = 1,
        int displayOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Concept
        {
            Id = id.ToLowerInvariant(),
            Name = name,
            ParentId = parentId?.ToLowerInvariant(),
            Description = description,
            Difficulty = Math.Clamp(difficulty, 1, 5),
            DisplayOrder = displayOrder
        };
    }

    public void SetEmbedding(ReadOnlyMemory<float> embedding)
    {
        Embedding = embedding;
    }
}
```

**ConceptPrerequisite.cs**
```csharp
public sealed class ConceptPrerequisite
{
    public string ConceptId { get; private set; }
    public string PrerequisiteId { get; private set; }
    public decimal Strength { get; private set; }  // 0.5 = helpful, 1.0 = required

    public Concept Concept { get; private set; }
    public Concept Prerequisite { get; private set; }

#pragma warning disable CS8618
    private ConceptPrerequisite() { }
#pragma warning restore CS8618

    public static ConceptPrerequisite Create(
        string conceptId,
        string prerequisiteId,
        decimal strength = 1.0m)
    {
        return new ConceptPrerequisite
        {
            ConceptId = conceptId.ToLowerInvariant(),
            PrerequisiteId = prerequisiteId.ToLowerInvariant(),
            Strength = Math.Clamp(strength, 0.1m, 1.0m)
        };
    }
}
```

**UserSkillState.cs**
```csharp
public sealed class UserSkillState
{
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public string ConceptId { get; private set; }

    public decimal MasteryProbability { get; private set; }
    public int ExposureCount { get; private set; }
    public int DemonstratedCount { get; private set; }
    public int StruggledCount { get; private set; }

    public DateTimeOffset? FirstExposure { get; private set; }
    public DateTimeOffset? LastPracticed { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public LearnerProfile Profile { get; private set; }
    public Concept Concept { get; private set; }

#pragma warning disable CS8618
    private UserSkillState() { }
#pragma warning restore CS8618

    public static UserSkillState Create(Guid profileId, string conceptId)
    {
        return new UserSkillState
        {
            ProfileId = profileId,
            ConceptId = conceptId.ToLowerInvariant(),
            MasteryProbability = 0.3m,  // Prior
            ExposureCount = 0,
            DemonstratedCount = 0,
            StruggledCount = 0,
            FirstExposure = null,
            LastPracticed = null,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void ApplySignal(SkillSignalType signalType, decimal confidence)
    {
        // Update mastery using Bayesian update
        MasteryProbability = SkillStateUpdater.UpdateMastery(
            MasteryProbability, signalType, confidence);

        // Update counts
        ExposureCount++;
        if (IsPositiveSignal(signalType))
            DemonstratedCount++;
        else
            StruggledCount++;

        // Update timestamps
        FirstExposure ??= DateTimeOffset.UtcNow;
        LastPracticed = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static bool IsPositiveSignal(SkillSignalType type) =>
        type is SkillSignalType.DemonstratedUnderstanding
            or SkillSignalType.AppliedSuccessfully
            or SkillSignalType.CorrectedCody
            or SkillSignalType.AskedAdvancedQuestion
            or SkillSignalType.ReferencedCorrectly;
}
```

### 6.2 EF Core Configuration

**ConceptConfiguration.cs**
```csharp
public sealed class ConceptConfiguration : IEntityTypeConfiguration<Concept>
{
    public void Configure(EntityTypeBuilder<Concept> builder)
    {
        builder.ToTable("Concepts", "nova");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength(200);

        builder.Property(c => c.ParentId).HasMaxLength(200);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.Difficulty).IsRequired();
        builder.Property(c => c.DisplayOrder).IsRequired();

        // Embedding stored as varbinary (1536 floats * 4 bytes = 6144 bytes)
        builder.Property(c => c.Embedding)
            .HasConversion(
                v => v.HasValue ? MemoryMarshal.AsBytes(v.Value.Span).ToArray() : null,
                v => v != null ? new ReadOnlyMemory<float>(MemoryMarshal.Cast<byte, float>(v).ToArray()) : null)
            .HasColumnType("varbinary(6200)");

        // Self-referential relationship
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.ParentId);
        builder.HasIndex(c => c.Difficulty);
    }
}
```

**ConceptPrerequisiteConfiguration.cs**
```csharp
public sealed class ConceptPrerequisiteConfiguration : IEntityTypeConfiguration<ConceptPrerequisite>
{
    public void Configure(EntityTypeBuilder<ConceptPrerequisite> builder)
    {
        builder.ToTable("ConceptPrerequisites", "nova");

        builder.HasKey(cp => new { cp.ConceptId, cp.PrerequisiteId });

        builder.Property(cp => cp.ConceptId).HasMaxLength(200);
        builder.Property(cp => cp.PrerequisiteId).HasMaxLength(200);
        builder.Property(cp => cp.Strength).HasPrecision(3, 2);

        builder.HasOne(cp => cp.Concept)
            .WithMany(c => c.Prerequisites)
            .HasForeignKey(cp => cp.ConceptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.Prerequisite)
            .WithMany(c => c.DependentConcepts)
            .HasForeignKey(cp => cp.PrerequisiteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**UserSkillStateConfiguration.cs**
```csharp
public sealed class UserSkillStateConfiguration : IEntityTypeConfiguration<UserSkillState>
{
    public void Configure(EntityTypeBuilder<UserSkillState> builder)
    {
        builder.ToTable("UserSkillStates", "nova");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.ConceptId).HasMaxLength(200).IsRequired();
        builder.Property(s => s.MasteryProbability).HasPrecision(5, 4);
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasOne(s => s.Profile)
            .WithMany()
            .HasForeignKey(s => s.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Concept)
            .WithMany()
            .HasForeignKey(s => s.ConceptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one skill state per user per concept
        builder.HasIndex(s => new { s.ProfileId, s.ConceptId }).IsUnique();

        // Query optimization
        builder.HasIndex(s => new { s.ProfileId, s.MasteryProbability });
        builder.HasIndex(s => new { s.ProfileId, s.LastPracticed });
    }
}
```

---

## 7. Application Layer (CQRS Handlers)

### 7.1 ExtractSkillSignals Command

Extends the existing memory extraction to also extract skill signals.

```csharp
public sealed record ExtractSkillSignalsCommand(
    Guid ProfileId,
    Guid SessionId,
    IReadOnlyList<MessageDto> Messages) : ICommand<ExtractSkillSignalsResult>;

public sealed record ExtractSkillSignalsResult(
    int SignalsExtracted,
    int SkillStatesUpdated);
```

### 7.2 GetSkillsForContext Query

```csharp
public sealed record GetSkillsForContextQuery(
    Guid ProfileId,
    string UserMessage) : IQuery<SkillContextDto>;

public sealed record SkillContextDto(
    IReadOnlyList<SkillStateDto> RelevantSkills,
    IReadOnlyList<SkillStateDto> MasteredSkills,
    IReadOnlyList<SkillGapDto> Gaps);

public sealed record SkillStateDto(
    string ConceptId,
    string ConceptName,
    decimal MasteryProbability,
    int ExposureCount,
    DateTimeOffset? LastPracticed);

public sealed record SkillGapDto(
    string ConceptName,
    string MissingPrerequisite);
```

### 7.3 Integration with SendMessage

Update `SendMessage` handler to include skill context:

```csharp
// In SendMessage handler, before calling LLM:

// Get skill context for this message
var skillContext = await mediator.SendAsync(
    new GetSkillsForContextQuery(profileId, request.Message),
    cancellationToken);

// Build personalized prompt with profile, memories, AND skills
var systemPrompt = SystemPrompts.BuildPersonalizedPrompt(
    profile,
    memories,
    skillContext);  // NEW
```

---

## 8. Repository Interfaces

### 8.1 IConceptRepository

```csharp
public interface IConceptRepository
{
    Task<Concept?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Concept>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Concept>> GetChildrenAsync(string parentId, CancellationToken ct = default);
    Task<IReadOnlyList<Concept>> GetWithPrerequisitesAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Concept>> FindSimilarAsync(ReadOnlyMemory<float> embedding, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<Concept>> SearchByKeywordsAsync(IEnumerable<string> keywords, int limit, CancellationToken ct = default);

    void Add(Concept concept);
    void AddRange(IEnumerable<Concept> concepts);
}
```

### 8.2 ISkillStateRepository

```csharp
public interface ISkillStateRepository
{
    Task<UserSkillState?> GetAsync(Guid profileId, string conceptId, CancellationToken ct = default);
    Task<IReadOnlyList<UserSkillState>> GetForConceptsAsync(Guid profileId, IEnumerable<string> conceptIds, CancellationToken ct = default);
    Task<IReadOnlyList<UserSkillState>> GetTopMasteredAsync(Guid profileId, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<UserSkillState>> GetRecentlyPracticedAsync(Guid profileId, int days, int limit, CancellationToken ct = default);

    void Add(UserSkillState state);
    void Update(UserSkillState state);
}
```

---

## 9. Implementation Order

### Phase 3.1: Foundation (Database + Entities)
1. Create domain entities (Concept, ConceptPrerequisite, UserSkillState)
2. Create EF Core configurations
3. Create and apply migration
4. Add repository interfaces and implementations
5. Register in DI

### Phase 3.2: Taxonomy Seeding
1. Create JSON taxonomy file with initial concepts
2. Create ConceptSeeder to populate database
3. Generate embeddings for all concepts
4. Store prerequisites relationships

### Phase 3.3: Signal Extraction
1. Create SkillSignal value object and enum
2. Create ExtractSkillSignals command/handler
3. Extend TriggerMemoryExtraction to also extract signals
4. Implement Bayesian update logic in UserSkillState

### Phase 3.4: Skill-Aware Prompting
1. Create GetSkillsForContext query/handler
2. Update SystemPrompts to include skill context
3. Update SendMessage to fetch and inject skill context
4. Test with different skill levels

### Phase 3.5: Testing & Tuning
1. Manual testing with conversations
2. Adjust Bayesian parameters based on observed behavior
3. Add telemetry for signal extraction accuracy
4. Tune prompt for better signal extraction

---

## 10. Success Criteria

- [ ] Cody's explanation depth matches user's skill level
- [ ] Users with mastered skills get concise responses (no over-explaining)
- [ ] Users with novice skills get detailed, scaffolded explanations
- [ ] Signal extraction identifies correct concepts from conversations
- [ ] Mastery probabilities update appropriately after conversations
- [ ] Gap identification works (missing prerequisites are surfaced)
- [ ] Context assembly latency < 500ms (including skill lookup)

---

## 11. Future Considerations (Phase 4)

Not in scope for Phase 3, but designed to enable:

- **Spaced repetition**: UserSkillState.NextReviewDue for resurfacing concepts
- **Learning paths**: Use prerequisites graph to generate ordered learning sequences
- **Proactive suggestions**: "You've mastered X, ready for Y?"
- **Progress dashboard**: Visualize skill tree with mastery levels

---

## 12. Open Questions

1. **Taxonomy granularity**: How deep should we go? (e.g., "csharp.linq.where" vs just "csharp.linq.basics")
2. **Cold start**: What mastery to assume for new users? (Currently 0.3)
3. **Decay**: Should mastery decay over time without practice? (Not implemented yet)
4. **Confidence calibration**: How to tune the Bayesian parameters?
5. **Multi-concept messages**: How to handle when user discusses multiple concepts at once?
