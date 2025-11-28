using Codewrinkles.Domain.Identity;
using Codewrinkles.Domain.Social.Exceptions;

namespace Codewrinkles.Domain.Social;

/// <summary>
/// Represents a follow relationship between two profiles.
/// Composite primary key: (FollowerId, FollowingId)
/// </summary>
public sealed class Follow
{
    // Private parameterless constructor for EF Core materialization only
    // EF Core will populate all properties via reflection when loading from database
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private Follow() { }
#pragma warning restore CS8618

    // Properties - Composite Primary Key
    public Guid FollowerId { get; private set; }   // Who is following
    public Guid FollowingId { get; private set; }  // Who is being followed
    public DateTime CreatedAt { get; private set; }

    // Navigation properties (cross-domain reference to Identity.Profile)
    public Profile Follower { get; private set; }
    public Profile Following { get; private set; }

    // Factory method
    public static Follow Create(Guid followerId, Guid followingId)
    {
        ArgumentException.ThrowIfNullOrEmpty(followerId.ToString(), nameof(followerId));
        ArgumentException.ThrowIfNullOrEmpty(followingId.ToString(), nameof(followingId));

        if (followerId == followingId)
        {
            throw new FollowSelfException("Cannot follow yourself");
        }

        return new Follow
        {
            FollowerId = followerId,
            FollowingId = followingId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
