namespace Codewrinkles.Infrastructure.Configuration;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string ApiKey { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Codewrinkles";
    public string BaseUrl { get; init; } = "https://codewrinkles.com";

    /// <summary>
    /// Hour in UTC when re-engagement job runs (0-23). Default: 4 (4 AM UTC)
    /// </summary>
    public int ReengagementHourUtc { get; init; } = 4;

    /// <summary>
    /// Maximum emails to send per re-engagement run. Default: 100
    /// </summary>
    public int ReengagementBatchSize { get; init; } = 100;
}
