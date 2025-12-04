using System.Diagnostics;

namespace Codewrinkles.Telemetry;

/// <summary>
/// Central definitions for all ActivitySources used throughout the application.
/// ActivitySources should be created once and reused - they are expensive to create.
/// </summary>
public static class ActivitySources
{
    /// <summary>
    /// ActivitySource for Application layer operations (handlers, validators, use cases).
    /// </summary>
    public static readonly ActivitySource Application = new("Codewrinkles.Application");

    /// <summary>
    /// ActivitySource for Infrastructure layer operations (repositories, external services).
    /// </summary>
    public static readonly ActivitySource Infrastructure = new("Codewrinkles.Infrastructure");

    /// <summary>
    /// All ActivitySource names for registration with OpenTelemetry.
    /// </summary>
    public static readonly string[] AllSourceNames =
    [
        "Codewrinkles.Application",
        "Codewrinkles.Infrastructure"
    ];
}
