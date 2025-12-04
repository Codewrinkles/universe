using System.Diagnostics.Metrics;

namespace Codewrinkles.Telemetry;

/// <summary>
/// Central definitions for all Meters used throughout the application.
/// Meters should be created once and reused - they are expensive to create.
/// </summary>
public static class Meters
{
    /// <summary>
    /// Meter for business metrics (user registrations, pulses created, engagements, etc.).
    /// </summary>
    public static readonly Meter Business = new("Codewrinkles.Metrics.Business");

    /// <summary>
    /// Meter for technical metrics (handler durations, validation durations, etc.).
    /// </summary>
    public static readonly Meter Technical = new("Codewrinkles.Metrics.Technical");

    /// <summary>
    /// All Meter names for registration with OpenTelemetry.
    /// </summary>
    public static readonly string[] AllMeterNames =
    [
        "Codewrinkles.Metrics.Business",
        "Codewrinkles.Metrics.Technical"
    ];
}
