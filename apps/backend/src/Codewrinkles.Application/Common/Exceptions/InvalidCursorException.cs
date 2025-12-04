namespace Codewrinkles.Application.Common.Exceptions;

/// <summary>
/// Thrown when a pagination cursor has an invalid format and cannot be parsed.
/// </summary>
public sealed class InvalidCursorException : Exception
{
    public InvalidCursorException()
        : base("The cursor format is invalid.")
    {
    }

    public InvalidCursorException(string cursor)
        : base($"The cursor '{cursor}' has an invalid format.")
    {
        Cursor = cursor;
    }

    public string? Cursor { get; }
}
