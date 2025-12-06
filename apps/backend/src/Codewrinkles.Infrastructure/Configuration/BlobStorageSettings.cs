namespace Codewrinkles.Infrastructure.Configuration;

public sealed class BlobStorageSettings
{
    public const string SectionName = "BlobStorage";

    public string AccountName { get; init; } = string.Empty;
    public string AvatarContainer { get; init; } = string.Empty;
    public string PulseImageContainer { get; init; } = string.Empty;
    public bool UseManagedIdentity { get; init; }
    public string? ConnectionString { get; init; }

    public string GetBlobStorageUrl() => $"https://{AccountName}.blob.core.windows.net";
}
