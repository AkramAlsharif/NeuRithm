namespace Neurithm.Core.Models;

/// <summary>
/// Represents a single note event in a level.
/// </summary>
public record LevelNote
{
    /// <summary>
    /// Note name (e.g., "E4", "C4").
    /// </summary>
    public required string NoteName { get; init; }

    /// <summary>
    /// Beat number where this note should be played.
    /// </summary>
    public double Beat { get; init; }

    /// <summary>
    /// Duration in beats (for visual feedback).
    /// </summary>
    public double DurationBeats { get; init; } = 1.0;
}

/// <summary>
/// Represents a level/song in the game.
/// Data-driven: loaded from JSON files.
/// </summary>
public record GameLevel
{
    /// <summary>
    /// Unique identifier for this level (e.g., "beethoven_ode_to_joy_easy").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Composer/creator name.
    /// </summary>
    public string Composer { get; init; } = string.Empty;

    /// <summary>
    /// Difficulty level (e.g., "Beginner", "Intermediate", "Advanced").
    /// </summary>
    public string Difficulty { get; init; } = "Beginner";

    /// <summary>
    /// Tempo in beats per minute.
    /// </summary>
    public int BPM { get; init; } = 120;

    /// <summary>
    /// Time signature (e.g., "4/4").
    /// </summary>
    public string TimeSignature { get; init; } = "4/4";

    /// <summary>
    /// Multiplier for normal tempo (1.0 = standard).
    /// </summary>
    public double NormalTempoMultiplier { get; init; } = 1.0;

    /// <summary>
    /// Preview text for level selection.
    /// </summary>
    public string PreviewText { get; init; } = string.Empty;

    /// <summary>
    /// The notes that make up this level.
    /// </summary>
    public List<LevelNote> Notes { get; init; } = [];

    /// <summary>
    /// Schema version for future compatibility.
    /// </summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>
    /// Calculate the estimated duration of the level in seconds.
    /// </summary>
    public double GetDurationSeconds()
    {
        if (Notes.Count == 0) return 0;
        var lastNoteBeat = Notes.Max(n => n.Beat + n.DurationBeats);
        var beatsPerSecond = BPM / 60.0;
        return lastNoteBeat / beatsPerSecond;
    }
}
