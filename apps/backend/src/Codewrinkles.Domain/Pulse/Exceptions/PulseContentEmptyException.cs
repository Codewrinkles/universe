namespace Codewrinkles.Domain.Pulse.Exceptions;

public sealed class PulseContentEmptyException : Exception
{
    public PulseContentEmptyException()
        : base("Pulse content cannot be empty or whitespace.")
    {
    }
}
