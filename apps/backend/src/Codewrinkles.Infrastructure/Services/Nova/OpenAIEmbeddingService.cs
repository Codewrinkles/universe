using System.Buffers.Binary;
using Codewrinkles.Application.Common.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace Codewrinkles.Infrastructure.Services.Nova;

/// <summary>
/// Embedding service implementation using Microsoft Semantic Kernel with OpenAI.
/// </summary>
public sealed class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public OpenAIEmbeddingService(Kernel kernel)
    {
        _embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
    }

    public async Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateAsync(
            text,
            cancellationToken: cancellationToken);

        return embedding.Vector.ToArray();
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
