namespace Neurithm.Core.Models;

/// <summary>
/// Represents a musical note with pitch information.
/// </summary>
public record Note
{
    /// <summary>
    /// Note name in scientific notation (e.g., "C4", "A4", "B2").
    /// </summary>
    public required string NoteName { get; init; }

    /// <summary>
    /// MIDI note number (0-127), where 60 = C4 (middle C).
    /// </summary>
    public int MidiNumber { get; init; }

    /// <summary>
    /// Frequency in Hz.
    /// </summary>
    public double Frequency { get; init; }

    /// <summary>
    /// Cents offset from the note's ideal frequency.
    /// Positive = sharp, negative = flat.
    /// </summary>
    public double CentsOffset { get; init; } = 0;

    /// <summary>
    /// Confidence score (0-1) indicating how certain the pitch detection is.
    /// </summary>
    public double Confidence { get; init; } = 0;

    /// <summary>
    /// Input volume/amplitude (0-1).
    /// </summary>
    public double Volume { get; init; } = 0;

    /// <summary>
    /// Timestamp when this note was detected (milliseconds).
    /// </summary>
    public long TimestampMs { get; init; }
}
