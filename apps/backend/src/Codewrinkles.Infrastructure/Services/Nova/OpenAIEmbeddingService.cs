using System.Buffers.Binary;
using System.ClientModel;
using Codewrinkles.Application.Common.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Embedding service implementation using Microsoft Semantic Kernel with OpenAI.
/// Includes retry logic with exponential backoff for transient errors (502, 503, etc.).
/// </summary>
public sealed class OpenAIEmbeddingService : IEmbeddingService
{
    private const int MaxRetries = 10;
    private const int InitialDelayMs = 1000; // 1 second

    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<OpenAIEmbeddingService> _logger;

    public OpenAIEmbeddingService(Kernel kernel, ILogger<OpenAIEmbeddingService> logger)
    {
        _embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        _logger = logger;
    }

    public async Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var delay = InitialDelayMs;

        while (true)
        {
            try
            {
                attempt++;
                var embedding = await _embeddingGenerator.GenerateAsync(
                    text,
                    cancellationToken: cancellationToken);

                return embedding.Vector.ToArray();
            }
            catch (ClientResultException ex) when (IsTransientError(ex) && attempt < MaxRetries)
            {
                _logger.LogWarning(
                    "Transient error (attempt {Attempt}/{MaxRetries}): {Status}. Retrying in {Delay}ms...",
                    attempt, MaxRetries, ex.Status, delay);

                await Task.Delay(delay, cancellationToken);
                delay *= 2; // Exponential backoff
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(
                    "Network error (attempt {Attempt}/{MaxRetries}): {Message}. Retrying in {Delay}ms...",
                    attempt, MaxRetries, ex.Message, delay);

                await Task.Delay(delay, cancellationToken);
                delay *= 2;
            }
        }
    }

    private static bool IsTransientError(ClientResultException ex)
    {
        // 502 Bad Gateway, 503 Service Unavailable, 504 Gateway Timeout
        // Also treat 429 (rate limit) as transient - wait and retry
        return ex.Status is 429 or 502 or 503 or 504;
    }

    public float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Embeddings must have the same dimension.");
        }

        float dotProduct = 0;
        float normA = 0;
        float normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denominator = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        if (denominator == 0)
        {
            return 0;
        }

        return dotProduct / denominator;
    }

    public byte[] SerializeEmbedding(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        for (int i = 0; i < embedding.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(
                bytes.AsSpan(i * sizeof(float)),
                embedding[i]);
        }
        return bytes;
    }

    public float[] DeserializeEmbedding(byte[] data)
    {
        var embedding = new float[data.Length / sizeof(float)];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = BinaryPrimitives.ReadSingleLittleEndian(
                data.AsSpan(i * sizeof(float)));
        }
        return embedding;
    }
}
