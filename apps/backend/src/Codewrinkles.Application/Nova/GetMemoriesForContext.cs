using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Domain.Nova;
using Codewrinkles.Telemetry;
using Kommand.Abstractions;

namespace Codewrinkles.Application.Nova;

public sealed record GetMemoriesForContextQuery(
    Guid ProfileId,
    string CurrentMessage
) : ICommand<GetMemoriesForContextResult>;

public sealed record GetMemoriesForContextResult(
    IReadOnlyList<MemoryDto> Memories,
    string FormattedContext
);

public sealed record MemoryDto(
    Guid Id,
    string Category,
    string Content,
    int Importance,
    DateTimeOffset CreatedAt
);

public sealed class GetMemoriesForContextQueryHandler
    : ICommandHandler<GetMemoriesForContextQuery, GetMemoriesForContextResult>
{
    private const int RecentMemoriesCount = 5;
    private const int HighImportanceThreshold = 4;
    private const int HighImportanceLimit = 5;
    private const int SemanticSearchLimit = 10;
    private const int MaxTotalMemories = 20;
    private const float MinSemanticSimilarity = 0.7f;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmbeddingService _embeddingService;

    public GetMemoriesForContextQueryHandler(
        IUnitOfWork unitOfWork,
        IEmbeddingService embeddingService)
    {
        _unitOfWork = unitOfWork;
        _embeddingService = embeddingService;
    }

    public async Task<GetMemoriesForContextResult> HandleAsync(
        GetMemoriesForContextQuery query,
        CancellationToken cancellationToken)
    {
        using var activity = TelemetryExtensions.StartApplicationActivity(SpanNames.Nova.GetMemoriesForContext);
        activity?.SetProfileId(query.ProfileId);

        try
        {
            // 1. Get recent memories
            var recentMemories = await _unitOfWork.NovaMemories.GetRecentAsync(
                query.ProfileId,
                RecentMemoriesCount,
                cancellationToken);

            // 2. Get high-importance memories
            var highImportanceMemories = await _unitOfWork.NovaMemories.GetByMinImportanceAsync(
                query.ProfileId,
                HighImportanceThreshold,
                HighImportanceLimit,
                cancellationToken);

            // 3. Get semantically relevant memories
            var semanticMemories = await GetSemanticallySimilarMemoriesAsync(
                query.ProfileId,
                query.CurrentMessage,
                cancellationToken);

            // 4. Merge and deduplicate
            var allMemories = MergeAndDeduplicate(
                recentMemories,
                highImportanceMemories,
                semanticMemories);

            // 5. Limit to max total
            var finalMemories = allMemories
                .Take(MaxTotalMemories)
                .Select(m => new MemoryDto(
                    m.Memory.Id,
                    m.Memory.Category.ToString(),
                    m.Memory.Content,
                    m.Memory.Importance,
                    m.Memory.CreatedAt))
                .ToList();

            // 6. Build formatted context
            var formattedContext = SystemPrompts.BuildMemoryContext(finalMemories);

            activity?.SetSuccess(true);
            activity?.SetTag("memories.count", finalMemories.Count);

            return new GetMemoriesForContextResult(finalMemories, formattedContext);
        }
        catch (Exception ex)
        {
            activity?.RecordError(ex);
            throw;
        }
    }

    private async Task<List<(Memory Memory, float Similarity)>> GetSemanticallySimilarMemoriesAsync(
        Guid profileId,
        string currentMessage,
        CancellationToken cancellationToken)
    {
        // Get all memories with embeddings
        var memoriesWithEmbeddings = await _unitOfWork.NovaMemories.GetWithEmbeddingsAsync(
            profileId,
            cancellationToken);

        if (memoriesWithEmbeddings.Count == 0)
        {
            return [];
        }

        // Generate embedding for current message
        var messageEmbedding = await _embeddingService.GetEmbeddingAsync(
            currentMessage,
            cancellationToken);

        // Calculate similarity for each memory
        var scored = new List<(Memory Memory, float Similarity)>();

        foreach (var memory in memoriesWithEmbeddings)
        {
            if (memory.Embedding is null)
            {
                continue;
            }

            var memoryEmbedding = _embeddingService.DeserializeEmbedding(memory.Embedding);
            var similarity = _embeddingService.CosineSimilarity(messageEmbedding, memoryEmbedding);

            if (similarity >= MinSemanticSimilarity)
            {
                scored.Add((memory, similarity));
            }
        }

        // Return top N by similarity
        return scored
            .OrderByDescending(x => x.Similarity)
            .Take(SemanticSearchLimit)
            .ToList();
    }

    private static List<ScoredMemory> MergeAndDeduplicate(
        IReadOnlyList<Memory> recentMemories,
        IReadOnlyList<Memory> highImportanceMemories,
        List<(Memory Memory, float Similarity)> semanticMemories)
    {
        var seen = new HashSet<Guid>();
        var result = new List<ScoredMemory>();

        // Add semantic memories first (highest priority)
        foreach (var (memory, similarity) in semanticMemories.OrderByDescending(x => x.Similarity))
        {
            if (seen.Add(memory.Id))
            {
                result.Add(new ScoredMemory(memory, Score: similarity + 1)); // Boost for semantic match
            }
        }

        // Add high importance memories
        foreach (var memory in highImportanceMemories.OrderByDescending(m => m.Importance))
        {
            if (seen.Add(memory.Id))
            {
                result.Add(new ScoredMemory(memory, Score: memory.Importance / 5.0f));
            }
        }

        // Add recent memories
        for (var i = 0; i < recentMemories.Count; i++)
        {
            var memory = recentMemories[i];
            if (seen.Add(memory.Id))
            {
                // Score decreases by position (most recent = highest score)
                result.Add(new ScoredMemory(memory, Score: (recentMemories.Count - i) / (float)recentMemories.Count * 0.5f));
            }
        }

        // Sort by score descending
        return result.OrderByDescending(m => m.Score).ToList();
    }

    private sealed record ScoredMemory(Memory Memory, float Score);
}
