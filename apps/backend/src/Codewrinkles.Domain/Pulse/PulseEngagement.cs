namespace Codewrinkles.Domain.Pulse;

public sealed class PulseEngagement
{
    // Private parameterless constructor for EF Core materialization only
    // EF Core will populate all properties via reflection when loading from database
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private PulseEngagement() { }
#pragma warning restore CS8618

    // Properties
    public Guid PulseId { get; private set; }
    public int ReplyCount { get; private set; }
    public int RepulseCount { get; private set; }
    public int LikeCount { get; private set; }
    public long ViewCount { get; private set; }

    // Navigation property
    public Pulse Pulse { get; private set; }

    // Factory method
    public static PulseEngagement Create(Guid pulseId)
    {
        return new PulseEngagement
        {
            PulseId = pulseId,
            ReplyCount = 0,
            RepulseCount = 0,
            LikeCount = 0,
            ViewCount = 0
        };
    }

    // Public methods
    public void IncrementReplyCount()
    {
        ReplyCount++;
    }

    public void DecrementReplyCount()
    {
        if (ReplyCount > 0)
        {
            ReplyCount--;
        }
    }

    public void IncrementRepulseCount()
    {
        RepulseCount++;
    }

    public void DecrementRepulseCount()
    {
        if (RepulseCount > 0)
        {
            RepulseCount--;
        }
    }

    public void IncrementLikeCount()
    {
        LikeCount++;
    }

    public void DecrementLikeCount()
    {
        if (LikeCount > 0)
        {
            LikeCount--;
        }
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }
}
