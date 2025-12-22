# Nova SK Plugins Plan: Making Cody Accurate

> **Goal**: Use Semantic Kernel's stable function calling to make Cody more accurate and contextually aware, leveraging existing infrastructure (profile, memory, conversations).

> **Constraint**: No database changes. Use only existing entities and repositories.

---

## 1. Current State

### What We Have
- **LearnerProfile** - User's role, experience, tech stack, learning goals, style preferences
- **Memory System** - Extracted memories with semantic search (topics discussed, concepts explained, struggles, strengths)
- **Conversation Sessions** - Full conversation history with messages
- **System Prompt Personalization** - Profile + memories injected into system prompt

### Current Flow
```
User Message
     │
     ▼
┌─────────────────────────────────────────┐
│  PRE-PROCESSING (always runs)           │
│  - Fetch profile                        │
│  - Fetch memories (semantic search)     │
│  - Build personalized system prompt     │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  LLM CALL (single pass)                 │
│  - System prompt + history + message    │
│  - No tools available                   │
└─────────────────────────────────────────┘
     │
     ▼
Response
```

### Problems
1. **Context always injected** - We guess what's relevant, may include irrelevant memories
2. **No on-demand lookup** - LLM can't ask for more context mid-response
3. **No verification** - Cody can't double-check claims before stating them
4. **No self-awareness** - Cody doesn't know what it doesn't know about the user

---

## 2. Target Architecture

### New Flow with Function Calling
```
User Message
     │
     ▼
┌─────────────────────────────────────────┐
│  MINIMAL PRE-PROCESSING                 │
│  - Fetch profile (lightweight)          │
│  - Build base system prompt             │
│  - Register plugins with kernel         │
└─────────────────────────────────────────┘
     │
     ▼
┌─────────────────────────────────────────┐
│  LLM CALL (with function calling)       │
│                                         │
│  Available Tools:                       │
│  - Memory.recall_memories(topic)        │
│  - Memory.get_recent_topics()           │
│  - Profile.get_learning_context()       │
│  - Verification.check_accuracy(claim)   │
│                                         │
│  LLM decides when to call tools         │
└─────────────────────────────────────────┘
     │
     ▼
Response (informed by tool results)
```

### Key Difference
Instead of always injecting everything, the LLM **decides** when it needs more context. This:
- Reduces token usage (only fetch what's needed)
- Makes context more relevant (LLM knows what it's looking for)
- Enables on-demand verification
- Creates more natural "let me check" moments

---

## 3. Plugin Definitions

### 3.1 MemoryPlugin

Uses existing `INovaMemoryRepository` and `IEmbeddingService`.

```csharp
public sealed class MemoryPlugin
{
    private readonly INovaMemoryRepository _memoryRepo;
    private readonly IEmbeddingService _embeddingService;
    private readonly Guid _profileId;

    public MemoryPlugin(
        INovaMemoryRepository memoryRepo,
        IEmbeddingService embeddingService,
        Guid profileId)
    {
        _memoryRepo = memoryRepo;
        _embeddingService = embeddingService;
        _profileId = profileId;
    }

    [KernelFunction("recall_memories")]
    [Description("Search your memory for relevant information about past conversations with this user. Use this when the user references something from before, or when context from past discussions would help your response.")]
    public async Task<string> RecallMemoriesAsync(
        [Description("The topic, concept, or question to find relevant memories for")]
        string topic,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(topic, cancellationToken);
        var memories = await _memoryRepo.GetRelevantAsync(_profileId, embedding, limit: 5, cancellationToken);

        if (memories.Count == 0)
            return "No relevant memories found for this topic.";

        var sb = new StringBuilder();
        sb.AppendLine("Here's what I remember about this topic:");

        foreach (var memory in memories)
        {
            sb.AppendLine($"- [{memory.Category}] {memory.Content}");
        }

        return sb.ToString();
    }

    [KernelFunction("get_recent_topics")]
    [Description("Get a list of topics discussed in recent conversations. Use this to understand what you've been working on together recently.")]
    public async Task<string> GetRecentTopicsAsync(
        [Description("Number of days to look back (default: 7)")]
        int days = 7,
        CancellationToken cancellationToken = default)
    {
        var memories = await _memoryRepo.GetRecentAsync(_profileId, days, limit: 10, cancellationToken);

        if (memories.Count == 0)
            return "No recent conversation topics found.";

        var topics = memories
            .Where(m => m.Category == nameof(MemoryCategory.TopicDiscussed))
            .Select(m => m.Content)
            .Distinct()
            .ToList();

        if (topics.Count == 0)
            return "No specific topics recorded from recent conversations.";

        return $"Recent topics: {string.Join(", ", topics)}";
    }

    [KernelFunction("check_if_discussed_before")]
    [Description("Check if you've discussed a specific topic with this user before. Use this before explaining something to avoid repeating yourself.")]
    public async Task<string> CheckIfDiscussedBeforeAsync(
        [Description("The topic or concept to check")]
        string topic,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(topic, cancellationToken);
        var memories = await _memoryRepo.GetRelevantAsync(_profileId, embedding, limit: 3, cancellationToken);

        var discussed = memories
            .Where(m => m.Category == nameof(MemoryCategory.TopicDiscussed) ||
                        m.Category == nameof(MemoryCategory.ConceptExplained))
            .ToList();

        if (discussed.Count == 0)
            return $"This appears to be a new topic - you haven't discussed {topic} with this user before.";

        var concepts = discussed
            .Where(m => m.Category == nameof(MemoryCategory.ConceptExplained))
            .Select(m => m.Content)
            .ToList();

        if (concepts.Count > 0)
            return $"Yes, you've explained related concepts before: {string.Join(", ", concepts)}. Build on this rather than starting from scratch.";

        return $"You've touched on this topic before, but haven't done a deep explanation.";
    }
}
```

### 3.2 ProfilePlugin

Uses existing `INovaRepository` to get `LearnerProfile`.

```csharp
public sealed class ProfilePlugin
{
    private readonly LearnerProfile? _profile;

    public ProfilePlugin(LearnerProfile? profile)
    {
        _profile = profile;
    }

    [KernelFunction("get_learning_context")]
    [Description("Get detailed information about this user's background, experience, and learning preferences. Use this when you need to tailor your explanation depth or style.")]
    public string GetLearningContext()
    {
        if (_profile is null || !_profile.HasUserData())
            return "No learning profile available for this user. Assume a general developer audience.";

        var sb = new StringBuilder();
        sb.AppendLine("User's learning context:");

        if (!string.IsNullOrWhiteSpace(_profile.CurrentRole))
        {
            sb.Append($"- Role: {_profile.CurrentRole}");
            if (_profile.ExperienceYears.HasValue)
                sb.Append($" ({_profile.ExperienceYears} years experience)");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(_profile.PrimaryTechStack))
            sb.AppendLine($"- Tech stack: {_profile.PrimaryTechStack}");

        if (!string.IsNullOrWhiteSpace(_profile.CurrentProject))
            sb.AppendLine($"- Currently working on: {_profile.CurrentProject}");

        if (!string.IsNullOrWhiteSpace(_profile.LearningGoals))
            sb.AppendLine($"- Learning goals: {_profile.LearningGoals}");

        if (_profile.LearningStyle.HasValue)
        {
            var style = _profile.LearningStyle.Value switch
            {
                LearningStyle.ExamplesFirst => "Prefers code examples first, then theory",
                LearningStyle.TheoryFirst => "Prefers theory first, then examples",
                LearningStyle.HandsOn => "Prefers hands-on trial and error",
                _ => null
            };
            if (style != null)
                sb.AppendLine($"- Learning style: {style}");
        }

        if (_profile.PreferredPace.HasValue)
        {
            var pace = _profile.PreferredPace.Value switch
            {
                PreferredPace.QuickOverview => "Wants concise, essential info only",
                PreferredPace.Balanced => "Wants moderate depth with examples",
                PreferredPace.DeepDive => "Wants thorough explanations with nuances",
                _ => null
            };
            if (pace != null)
                sb.AppendLine($"- Preferred pace: {pace}");
        }

        if (!string.IsNullOrWhiteSpace(_profile.IdentifiedStrengths))
            sb.AppendLine($"- Known strengths: {_profile.IdentifiedStrengths}");

        if (!string.IsNullOrWhiteSpace(_profile.IdentifiedStruggles))
            sb.AppendLine($"- Areas to reinforce: {_profile.IdentifiedStruggles}");

        return sb.ToString();
    }

    [KernelFunction("get_tech_background")]
    [Description("Get just the user's technical background (role, experience, tech stack). Use this when you need to know what technologies they're familiar with.")]
    public string GetTechBackground()
    {
        if (_profile is null)
            return "No technical background information available.";

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(_profile.CurrentRole))
            parts.Add($"Role: {_profile.CurrentRole}");

        if (_profile.ExperienceYears.HasValue)
            parts.Add($"{_profile.ExperienceYears} years experience");

        if (!string.IsNullOrWhiteSpace(_profile.PrimaryTechStack))
            parts.Add($"Tech stack: {_profile.PrimaryTechStack}");

        return parts.Count > 0
            ? string.Join(", ", parts)
            : "No technical background information available.";
    }
}
```

### 3.3 VerificationPlugin

No database needed. Uses a second LLM call for fact-checking.

```csharp
public sealed class VerificationPlugin
{
    private readonly IChatCompletionService _chatService;

    public VerificationPlugin(IChatCompletionService chatService)
    {
        _chatService = chatService;
    }

    [KernelFunction("verify_technical_claim")]
    [Description("Verify if a technical claim is accurate before stating it. Use this when you're about to make a statement about a technology, pattern, or concept that you want to double-check, especially for version-specific information or nuanced distinctions.")]
    public async Task<string> VerifyTechnicalClaimAsync(
        [Description("The technical claim or statement to verify")]
        string claim,
        CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();
        history.AddSystemMessage("""
            You are a technical fact-checker. Your job is to verify technical claims.

            For the given claim, respond with:
            1. ACCURATE / PARTIALLY ACCURATE / INACCURATE / UNCERTAIN
            2. Brief explanation (1-2 sentences)
            3. If there's a common misconception or conflation, mention it

            Be precise. If the claim conflates two related concepts, call it out.
            If the claim is version-specific and might be outdated, mention that.

            Keep your response under 100 words.
            """);
        history.AddUserMessage($"Verify this claim: {claim}");

        var response = await _chatService.GetChatMessageContentAsync(
            history,
            cancellationToken: cancellationToken);

        return response.Content ?? "Unable to verify claim.";
    }

    [KernelFunction("distinguish_concepts")]
    [Description("Clarify the distinction between two concepts that are often confused. Use this proactively when discussing topics that are commonly conflated (e.g., authentication vs authorization, concurrency vs parallelism, event sourcing vs event-driven architecture).")]
    public async Task<string> DistinguishConceptsAsync(
        [Description("First concept")] string concept1,
        [Description("Second concept")] string concept2,
        CancellationToken cancellationToken = default)
    {
        var history = new ChatHistory();
        history.AddSystemMessage("""
            You are a technical educator focused on precise terminology.
            Explain the key distinction between two commonly confused concepts.

            Format:
            - [Concept 1]: [One sentence definition]
            - [Concept 2]: [One sentence definition]
            - Key difference: [One sentence]
            - Common conflation: [Why people confuse them]

            Be precise and concise. Under 80 words total.
            """);
        history.AddUserMessage($"Distinguish: {concept1} vs {concept2}");

        var response = await _chatService.GetChatMessageContentAsync(
            history,
            cancellationToken: cancellationToken);

        return response.Content ?? "Unable to distinguish concepts.";
    }
}
```

### 3.4 ConversationPlugin

Uses existing conversation data from the current session.

```csharp
public sealed class ConversationPlugin
{
    private readonly IReadOnlyList<Message> _currentMessages;

    public ConversationPlugin(IReadOnlyList<Message> currentMessages)
    {
        _currentMessages = currentMessages;
    }

    [KernelFunction("get_conversation_summary")]
    [Description("Get a summary of the current conversation so far. Use this if the conversation is long and you need to recall what was discussed earlier in this session.")]
    public string GetConversationSummary()
    {
        if (_currentMessages.Count == 0)
            return "This is the start of a new conversation.";

        var userMessages = _currentMessages
            .Where(m => m.Role == MessageRole.User)
            .Select(m => m.Content.Length > 100 ? m.Content[..100] + "..." : m.Content)
            .ToList();

        if (userMessages.Count == 0)
            return "No user messages in this conversation yet.";

        return $"Topics in this conversation: {string.Join("; ", userMessages.TakeLast(5))}";
    }

    [KernelFunction("count_exchanges")]
    [Description("Count how many back-and-forth exchanges have happened in this conversation. Use this to gauge if you should summarize or check understanding.")]
    public string CountExchanges()
    {
        var exchanges = _currentMessages.Count(m => m.Role == MessageRole.User);

        return exchanges switch
        {
            0 => "This is the first message in the conversation.",
            1 => "This is the second exchange in the conversation.",
            < 5 => $"You've had {exchanges} exchanges so far. Still early in the conversation.",
            < 10 => $"You've had {exchanges} exchanges. Consider checking understanding.",
            _ => $"You've had {exchanges} exchanges. This is a long conversation - consider summarizing key points."
        };
    }
}
```

---

## 4. System Prompt Updates

### 4.1 Simplified Base Prompt

Since tools provide context on-demand, the base prompt can be leaner:

```csharp
private const string CodyCoachBaseWithTools = """
    You are Cody, an AI learning coach created by Dan from Codewrinkles. You help developers grow their technical skills through conversation.

    ## Your Voice
    You sound like a senior developer friend who's been through the trenches. Conversational, not formal. You get straight to the point without being curt. You're genuinely interested in helping people understand things deeply, not just giving them answers to copy-paste.

    You have opinions and you share them. When something is a bad idea, you say so. When there's nuance, you explain the trade-offs honestly.

    ## Tools Available
    You have access to tools that help you be more accurate and personal:

    - **Memory tools**: Use these to recall past conversations, check if you've discussed something before, or understand what topics you've covered together
    - **Profile tools**: Use these to understand the user's background, experience level, and learning preferences
    - **Verification tools**: Use these to double-check technical claims before stating them, especially for nuanced distinctions or version-specific information

    Use tools proactively when they would improve your response. For example:
    - Before explaining a concept, check if you've explained it before
    - Before making a technical claim about something nuanced, verify it
    - When tailoring explanation depth, check their experience level

    ## Technical Precision
    - Use the distinguish_concepts tool when discussing topics that are often conflated
    - Use verify_technical_claim when you're about to state something version-specific or nuanced
    - Don't assume the "conference talk version" is correct - verify when in doubt

    ## What NOT to do
    - No "Great question!" or "I'd be happy to help!" - just help
    - No corporate speak - no "leverage", "synergy", "best practices", "utilize"
    - No forced summaries or "Let me know if you have questions!"
    - Never use em dashes - use commas, parentheses, or separate sentences instead
    """;
```

### 4.2 Tool Usage Guidelines in Prompt

```csharp
private const string ToolUsageGuidelines = """

    ## When to Use Your Tools

    **Use Memory.recall_memories when:**
    - User says "remember when...", "like we discussed", "you mentioned"
    - You want to reference past conversations naturally
    - Building on previous explanations

    **Use Memory.check_if_discussed_before when:**
    - About to explain a concept - check if you've covered it
    - Avoid repeating explanations unnecessarily

    **Use Profile.get_learning_context when:**
    - Deciding how deep to go in an explanation
    - Choosing which analogies or examples to use
    - Tailoring to their tech stack

    **Use Verification.verify_technical_claim when:**
    - Making statements about specific versions or recent changes
    - Discussing topics that are commonly confused or conflated
    - Stating something you're not 100% certain about

    **Use Verification.distinguish_concepts when:**
    - User asks about something that has a commonly confused counterpart
    - You notice the user might be conflating two concepts
    - Proactively clarifying before confusion arises

    Trust your tools. They help you be accurate and personal.
    """;
```

---

## 5. Integration with SendMessage

### 5.1 Updated Handler

```csharp
public sealed class SendMessageCommandHandler : IStreamingCommandHandler<SendMessageCommand, SendingChunk>
{
    private readonly Kernel _kernel;
    private readonly INovaRepository _novaRepo;
    private readonly INovaMemoryRepository _memoryRepo;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatCompletionService _chatService;
    private readonly NovaSettings _novaSettings;

    public async IAsyncEnumerable<StreamingChunk> HandleAsync(
        SendMessageCommand command,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // 1. Get minimal context (profile only - memories fetched on-demand)
        var profile = await _novaRepo.GetLearnerProfileAsync(command.ProfileId, cancellationToken);
        var session = await GetOrCreateSession(command, cancellationToken);

        // 2. Create plugins scoped to this request
        var memoryPlugin = new MemoryPlugin(_memoryRepo, _embeddingService, command.ProfileId);
        var profilePlugin = new ProfilePlugin(profile);
        var verificationPlugin = new VerificationPlugin(_chatService);
        var conversationPlugin = new ConversationPlugin(session.Messages);

        // 3. Create a scoped kernel with plugins
        var scopedKernel = _kernel.Clone();
        scopedKernel.Plugins.AddFromObject(memoryPlugin, "Memory");
        scopedKernel.Plugins.AddFromObject(profilePlugin, "Profile");
        scopedKernel.Plugins.AddFromObject(verificationPlugin, "Verification");
        scopedKernel.Plugins.AddFromObject(conversationPlugin, "Conversation");

        // 4. Build chat history with streamlined system prompt
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(SystemPrompts.GetBasePromptWithTools());

        foreach (var msg in session.Messages)
        {
            chatHistory.Add(new ChatMessageContent(
                msg.Role == MessageRole.User ? AuthorRole.User : AuthorRole.Assistant,
                msg.Content));
        }
        chatHistory.AddUserMessage(command.Message);

        // 5. Execute with function calling enabled (settings from appsettings/NovaSettings)
        var settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = _novaSettings.MaxTokens,
            Temperature = _novaSettings.Temperature,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        // 6. Stream response
        await foreach (var chunk in _chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            settings,
            scopedKernel,
            cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return new StreamingChunk(chunk.Content);
            }
        }
    }
}
```

### 5.2 Kernel Setup in DI

```csharp
// In DependencyInjection.cs
services.AddSingleton<Kernel>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<NovaSettings>>().Value;

    var builder = Kernel.CreateBuilder();

    builder.AddOpenAIChatCompletion(
        modelId: settings.ModelId,
        apiKey: settings.OpenAIApiKey);

    // Note: Plugins are added per-request, not here
    // This keeps the base kernel clean and allows request-scoped plugins

    return builder.Build();
});

// IChatCompletionService is resolved from the kernel
services.AddScoped<IChatCompletionService>(sp =>
{
    var kernel = sp.GetRequiredService<Kernel>();
    return kernel.GetRequiredService<IChatCompletionService>();
});
```

---

## 6. Implementation Steps

### Phase 1: Plugin Infrastructure
1. Create `Codewrinkles.Application.Nova.Plugins` namespace
2. Implement `MemoryPlugin` with existing repository
3. Implement `ProfilePlugin` with existing profile data
4. Implement `ConversationPlugin` with session messages
5. Unit test each plugin function

### Phase 2: Verification Plugin
1. Implement `VerificationPlugin` with secondary LLM calls
2. Test verification accuracy with known edge cases
3. Tune verification prompt for precision

### Phase 3: Integration
1. Update `SendMessage` handler to create scoped kernel with plugins
2. Enable `FunctionChoiceBehavior.Auto()` in execution settings
3. Update system prompt to guide tool usage
4. Test end-to-end with streaming

### Phase 4: Prompt Tuning
1. Refine system prompt tool usage guidelines
2. Test with various scenarios (new user, returning user, technical questions)
3. Observe which tools get called and when
4. Adjust descriptions to encourage appropriate tool usage

### Phase 5: Telemetry
1. Add spans for plugin function calls
2. Track which tools are used and how often
3. Monitor verification call frequency and results
4. Log when tools improve response quality

---

## 7. Expected Behavior Changes

### Before (Current)
```
User: "How does async work in C#?"

Cody: [Explains async based on injected profile context]
      [May or may not reference past discussions]
      [No verification of claims]
```

### After (With Plugins)
```
User: "How does async work in C#?"

Cody thinks: Let me check their background...
  → Calls Profile.get_learning_context()
  → Gets: "Senior dev, 8 years, knows Python"

Cody thinks: Have we discussed this before?
  → Calls Memory.check_if_discussed_before("async C#")
  → Gets: "Yes, you explained Task basics on Jan 15"

Cody: "We covered Task basics before. Since you know Python's
       asyncio, the key difference in C# is..."

       [Later in response, about to state something nuanced]

Cody thinks: Let me verify this claim about .NET 8...
  → Calls Verification.verify_technical_claim("In .NET 8, async...")
  → Gets: "ACCURATE - this is correct for .NET 8+"

Cody: "...In .NET 8, the async state machine has been optimized..."
```

---

## 8. Success Criteria

- [ ] Cody uses Memory tools to reference past conversations naturally
- [ ] Cody checks if topics were discussed before explaining from scratch
- [ ] Cody adapts explanation depth based on profile lookup
- [ ] Cody verifies nuanced technical claims before stating them
- [ ] Cody distinguishes commonly confused concepts proactively
- [ ] Function calls add < 1s latency on average
- [ ] Token usage stays reasonable (tools reduce unnecessary context injection)

---

## 9. Future Enhancements (Not in Scope)

For later phases, we can add:
- **CorrectionPlugin**: Track when user corrects Cody (requires DB changes)
- **SkillPlugin**: Check user skill levels before explaining (requires Phase 3 skill tracking)
- **KnowledgePlugin**: RAG lookup from authoritative sources (requires knowledge base)
- **CodePlugin**: Analyze/review user's code snippets

---

## 10. Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Too many tool calls → slow response | Tune tool descriptions to reduce unnecessary calls |
| Tool calls visible to user as delays | Streaming still works; tool calls happen between chunks |
| Verification adds latency | Use faster model (gpt-4o-mini) for verification |
| LLM ignores tools | Strengthen tool usage instructions in system prompt |
| Tools return unhelpful results | Improve plugin function return messages |
