namespace Codewrinkles.Domain.Social.Exceptions;

public sealed class NotFollowingException : Exception
{
    public NotFollowingException(Guid followerId, Guid followingId)
        : base($"Profile {followerId} is not following {followingId}") { }
}
