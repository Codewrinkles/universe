using Codewrinkles.Application.Common.Interfaces;
using Codewrinkles.Application.Nova.Services;
using Codewrinkles.Domain.Nova;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Implementation of content search using in-memory similarity calculation.
/// For Alpha/Beta, this loads all embeddings and calculates similarity in memory.
/// Can be migrated to Azure AI Search or SQL Server 2025 native vector when needed.
/// </summary>
public sealed class ContentSearchService : IContentSearchService
{
    private readonly IContentChunkRepository _repository;
    private readonly IEmbeddingService _embeddingService;

    public ContentSearchService(
        IContentChunkRepository repository,
        IEmbeddingService embeddingService)
    {
        _repository = repository;
        _embeddingService = embeddingService;
    }

    public async Task<IReadOnlyList<ContentSearchResult>> SearchAsync(
        string query,
        ContentSource? source = null,
        string? technology = null,
        string? author = null,
        int limit = 5,
        float minSimilarity = 0.7f,
        CancellationToken cancellationToken = default)
    {
        // Generate embedding for query
        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);

        // Get all matching chunks with embeddings
        var chunks = await _repository.GetWithEmbeddingsAsync(
            source,
            technology,
            author,
            cancellationToken);

        // Calculate similarity and filter
        var scored = new List<(ContentSearchResult Result, float Similarity)>();

        foreach (var chunk in chunks)
        {
            var chunkEmbedding = _embeddingService.DeserializeEmbedding(chunk.Embedding);
            var similarity = _embeddingService.CosineSimilarity(queryEmbedding, chunkEmbedding);

            if (similarity >= minSimilarity)
            {
                scored.Add((new ContentSearchResult(
                    chunk.Id,
                    chunk.Source,
                    chunk.Title,
                    chunk.Content,
                    chunk.Author,
                    chunk.Technology,
                    similarity), similarity));
            }
        }

        // Sort by similarity and take top N
        return scored
            .OrderByDescending(x => x.Similarity)
            .Take(limit)
            .Select(x => x.Result)
            .ToList();
    }
}
