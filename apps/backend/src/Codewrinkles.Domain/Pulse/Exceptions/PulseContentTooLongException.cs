namespace Codewrinkles.Domain.Pulse.Exceptions;

public sealed class PulseContentTooLongException : Exception
{
    public PulseContentTooLongException(int actualLength, int maxLength = 500)
        : base($"Pulse content length ({actualLength}) exceeds maximum allowed length ({maxLength}).")
    {
        ActualLength = actualLength;
        MaxLength = maxLength;
    }

    public int ActualLength { get; }
    public int MaxLength { get; }
}
