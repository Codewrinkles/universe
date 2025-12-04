namespace Codewrinkles.Application.Common.Exceptions;

/// <summary>
/// Thrown when attempting to create content (pulse, reply, etc.) for a non-existent author profile.
/// </summary>
public sealed class AuthorNotFoundException : Exception
{
    public AuthorNotFoundException(Guid authorId)
        : base($"Author profile with ID '{authorId}' was not found.")
    {
        AuthorId = authorId;
    }

    public Guid AuthorId { get; }
}
