namespace Neurithm.Core.Models;

/// <summary>
/// Represents a user's game profile.
/// </summary>
public record UserProfile
{
    /// <summary>
    /// Unique identifier for this profile.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name chosen by the user.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Selected avatar ID.
    /// </summary>
    public string SelectedAvatarId { get; init; } = "female_aria";

    /// <summary>
    /// Preferred difficulty level.
    /// </summary>
    public string PreferredDifficulty { get; init; } = "Beginner";

    /// <summary>
    /// A4 tuning frequency in Hz (default 440).
    /// </summary>
    public double A4TuningHz { get; init; } = 440.0;

    /// <summary>
    /// Microphone sensitivity preset (0-1).
    /// </summary>
    public double MicrophoneSensitivity { get; init; } = 0.7;

    /// <summary>
    /// Latency offset in milliseconds.
    /// </summary>
    public int LatencyOffsetMs { get; init; } = 0;

    /// <summary>
    /// Last played level ID.
    /// </summary>
    public string? LastPlayedLevelId { get; init; }

    /// <summary>
    /// Total cumulative score across all attempts.
    /// </summary>
    public int TotalScore { get; init; } = 0;

    /// <summary>
    /// Best combo achieved.
    /// </summary>
    public int BestCombo { get; init; } = 0;

    /// <summary>
    /// Average accuracy percentage.
    /// </summary>
    public double AverageAccuracy { get; init; } = 0;

    /// <summary>
    /// Total completed levels (unique level IDs completed at least once).
    /// </summary>
    public int TotalCompletedLevels { get; init; } = 0;

    /// <summary>
    /// Created timestamp (UTC).
    /// </summary>
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp (UTC).
    /// </summary>
    public DateTime ModifiedUtc { get; init; } = DateTime.UtcNow;
}
