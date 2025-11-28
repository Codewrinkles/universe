namespace Codewrinkles.Domain.Social.Exceptions;

public sealed class AlreadyFollowingException : Exception
{
    public AlreadyFollowingException(Guid followerId, Guid followingId)
        : base($"Profile {followerId} is already following {followingId}") { }
}
