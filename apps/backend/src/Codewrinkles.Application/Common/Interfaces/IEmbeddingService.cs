namespace Codewrinkles.Application.Common.Interfaces;

/// <summary>
/// Service for generating text embeddings and computing similarity.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding as a float array.</returns>
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compute cosine similarity between two embedding vectors.
    /// </summary>
    /// <param name="a">First embedding.</param>
    /// <param name="b">Second embedding.</param>
    /// <returns>Similarity score between -1 and 1 (1 = identical).</returns>
    float CosineSimilarity(float[] a, float[] b);

    /// <summary>
    /// Serialize embedding to bytes for database storage.
    /// </summary>
    byte[] SerializeEmbedding(float[] embedding);

    /// <summary>
    /// Deserialize embedding from database storage.
    /// </summary>
    float[] DeserializeEmbedding(byte[] data);
}
