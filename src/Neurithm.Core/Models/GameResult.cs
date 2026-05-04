namespace Neurithm.Core.Models;

/// <summary>
/// Hit result feedback for a note.
/// </summary>
public enum HitResult
{
    Perfect = 3,
    Good = 2,
    Acceptable = 1,
    Miss = -1,
    Early = 0,
    Late = 0,
    NoAttempt = 0
}

/// <summary>
/// Represents the result of a single note hit during gameplay.
/// </summary>
public record NoteHitResult
{
    /// <summary>
    /// The note that was supposed to be hit.
    /// </summary>
    public required LevelNote ExpectedNote { get; init; }

    /// <summary>
    /// The note actually detected (if any).
    /// </summary>
    public Note? DetectedNote { get; init; }

    /// <summary>
    /// Timing offset in milliseconds (negative = early, positive = late).
    /// </summary>
    public double TimingOffsetMs { get; init; }

    /// <summary>
    /// The hit result classification.
    /// </summary>
    public HitResult Result { get; init; }

    /// <summary>
    /// Score gained or lost for this hit.
    /// </summary>
    public int ScoreDelta { get; init; }

    /// <summary>
    /// Timestamp when this was recorded (milliseconds into the level).
    /// </summary>
    public long GameTimestampMs { get; init; }
}

/// <summary>
/// Final result of a completed level attempt.
/// </summary>
public record GameResult
{
    /// <summary>
    /// Unique result ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Profile ID who played this level.
    /// </summary>
    public required string ProfileId { get; init; }

    /// <summary>
    /// Level ID that was played.
    /// </summary>
    public required string LevelId { get; init; }

    /// <summary>
    /// Final score.
    /// </summary>
    public int FinalScore { get; init; }

    /// <summary>
    /// Accuracy percentage (0-100).
    /// </summary>
    public double AccuracyPercent { get; init; }

    /// <summary>
    /// Maximum combo achieved.
    /// </summary>
    public int MaxCombo { get; init; }

    /// <summary>
    /// Number of perfect hits.
    /// </summary>
    public int PerfectCount { get; init; }

    /// <summary>
    /// Number of good hits.
    /// </summary>
    public int GoodCount { get; init; }

    /// <summary>
    /// Number of acceptable hits.
    /// </summary>
    public int AcceptableCount { get; init; }

    /// <summary>
    /// Number of misses.
    /// </summary>
    public int MissCount { get; init; }

    /// <summary>
    /// Game mode used (Practice, Level, FreePlay).
    /// </summary>
    public string GameMode { get; init; } = "Level";

    /// <summary>
    /// Completion timestamp (UTC).
    /// </summary>
    public DateTime CompletedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of the attempt in seconds.
    /// </summary>
    public double DurationSeconds { get; init; }

    /// <summary>
    /// Final tempo multiplier achieved.
    /// </summary>
    public double FinalTempoMultiplier { get; init; } = 1.0;

    /// <summary>
    /// Whether the level was completed successfully.
    /// </summary>
    public bool IsCompleted { get; init; } = true;

    /// <summary>
    /// All note hit results (for detailed analysis).
    /// </summary>
    public List<NoteHitResult> HitResults { get; init; } = [];
}
