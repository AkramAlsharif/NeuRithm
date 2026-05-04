namespace Neurithm.Core.Models;

/// <summary>
/// Configuration for pitch detection from audio.
/// </summary>
public record AudioCalibrationSettings
{
    /// <summary>
    /// A4 reference frequency in Hz.
    /// </summary>
    public double A4FrequencyHz { get; init; } = 440.0;

    /// <summary>
    /// Minimum frequency to detect (Hz).
    /// </summary>
    public double MinFrequencyHz { get; init; } = 32.7;  // C1

    /// <summary>
    /// Maximum frequency to detect (Hz).
    /// </summary>
    public double MaxFrequencyHz { get; init; } = 4186.0; // C8

    /// <summary>
    /// Confidence threshold (0-1). Detections below this are ignored.
    /// </summary>
    public double ConfidenceThreshold { get; init; } = 0.5;

    /// <summary>
    /// Noise gate threshold (volume). Quieter sounds are ignored.
    /// </summary>
    public double NoiseGateThreshold { get; init; } = 0.02;

    /// <summary>
    /// Latency offset in milliseconds to apply to hit detection.
    /// </summary>
    public int LatencyOffsetMs { get; init; } = 0;

    /// <summary>
    /// Audio buffer size for pitch detection (samples).
    /// </summary>
    public int AudioBufferSize { get; init; } = 4096;

    /// <summary>
    /// Sample rate (Hz). Typically 44100 or 48000.
    /// </summary>
    public int SampleRateHz { get; init; } = 44100;
}

/// <summary>
/// Represents timing and hit detection configuration for gameplay.
/// </summary>
public record GameTimingSettings
{
    /// <summary>
    /// Perfect hit window in milliseconds (±).
    /// </summary>
    public int PerfectWindowMs { get; init; } = 50;

    /// <summary>
    /// Good hit window in milliseconds (±).
    /// </summary>
    public int GoodWindowMs { get; init; } = 100;

    /// <summary>
    /// Acceptable hit window in milliseconds (±).
    /// </summary>
    public int AcceptableWindowMs { get; init; } = 150;

    /// <summary>
    /// Debounce time to prevent duplicate detections (ms).
    /// </summary>
    public int DebounceTimeMs { get; init; } = 100;
}

/// <summary>
/// Configuration for adaptive tempo system.
/// </summary>
public record AdaptiveTempoSettings
{
    /// <summary>
    /// Minimum tempo multiplier (slowest).
    /// </summary>
    public double MinTempoMultiplier { get; init; } = 0.5;

    /// <summary>
    /// Maximum tempo multiplier (fastest).
    /// </summary>
    public double MaxTempoMultiplier { get; init; } = 1.0;

    /// <summary>
    /// Amount to decrease tempo on miss.
    /// </summary>
    public double TempoDecreasePerMiss { get; init; } = 0.05;

    /// <summary>
    /// Amount to increase tempo on good performance.
    /// </summary>
    public double TempoIncreasePerGood { get; init; } = 0.02;

    /// <summary>
    /// Accuracy threshold to trigger tempo increase (0-100).
    /// </summary>
    public double AccuracyThresholdPercent { get; init; } = 80.0;

    /// <summary>
    /// Number of recent notes to consider for accuracy calculation.
    /// </summary>
    public int AccuracyWindowSize { get; init; } = 10;
}

/// <summary>
/// Scoring configuration.
/// </summary>
public record ScoringSettings
{
    /// <summary>
    /// Points for perfect hit.
    /// </summary>
    public int PerfectScore { get; init; } = 3;

    /// <summary>
    /// Points for good hit.
    /// </summary>
    public int GoodScore { get; init; } = 2;

    /// <summary>
    /// Points for acceptable hit.
    /// </summary>
    public int AcceptableScore { get; init; } = 1;

    /// <summary>
    /// Points deducted for miss.
    /// </summary>
    public int MissScore { get; init; } = -1;
}
