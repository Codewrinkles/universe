using System.Security.Cryptography;

namespace Codewrinkles.Domain.Nova;

/// <summary>
/// Represents an application for Nova Alpha access.
/// </summary>
public sealed class AlphaApplication
{
    // Private parameterless constructor for EF Core materialization only
#pragma warning disable CS8618
    private AlphaApplication() { }
#pragma warning restore CS8618

    // Properties
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string Name { get; private set; }
    public string PrimaryTechStack { get; private set; }
    public int YearsOfExperience { get; private set; }
    public string Goal { get; private set; }
    public AlphaApplicationStatus Status { get; private set; } = AlphaApplicationStatus.Pending;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    // Invite code (generated on approval)
    public string? InviteCode { get; private set; }
    public bool InviteCodeRedeemed { get; private set; }
    public Guid? RedeemedByProfileId { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }

    // Factory method
    public static AlphaApplication Create(
        string email,
        string name,
        string primaryTechStack,
        int yearsOfExperience,
        string goal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryTechStack, nameof(primaryTechStack));
        ArgumentException.ThrowIfNullOrWhiteSpace(goal, nameof(goal));

        if (yearsOfExperience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(yearsOfExperience), "Years of experience cannot be negative");
        }

        return new AlphaApplication
        {
            Email = email.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            PrimaryTechStack = primaryTechStack.Trim(),
            YearsOfExperience = yearsOfExperience,
            Goal = goal.Trim(),
            Status = AlphaApplicationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // Public methods
    public void Accept()
    {
        if (Status != AlphaApplicationStatus.Pending)
        {
            throw new InvalidOperationException("Can only accept pending applications");
        }

        Status = AlphaApplicationStatus.Accepted;
        InviteCode = GenerateInviteCode();
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void Waitlist()
    {
        if (Status != AlphaApplicationStatus.Pending)
        {
            throw new InvalidOperationException("Can only waitlist pending applications");
        }

        Status = AlphaApplicationStatus.Waitlisted;
        ReviewedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCodeRedeemed(Guid profileId)
    {
        if (Status != AlphaApplicationStatus.Accepted)
        {
            throw new InvalidOperationException("Can only redeem codes for accepted applications");
        }

        if (InviteCodeRedeemed)
        {
            throw new InvalidOperationException("Invite code has already been redeemed");
        }

        InviteCodeRedeemed = true;
        RedeemedByProfileId = profileId;
        RedeemedAt = DateTimeOffset.UtcNow;
    }

    // Private methods
    private static string GenerateInviteCode()
    {
        // Generate code like "NOVA-X7K9M2"
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude confusing chars (0, O, 1, I)
        var randomBytes = new byte[6];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var code = new char[6];
        for (var i = 0; i < 6; i++)
        {
            code[i] = chars[randomBytes[i] % chars.Length];
        }

        return $"NOVA-{new string(code)}";
    }
}
