namespace Codewrinkles.Domain.Social.Exceptions;

public sealed class FollowSelfException : Exception
{
    public FollowSelfException(string message) : base(message) { }
}
