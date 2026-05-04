namespace Neurithm.Application.DTOs;

/// <summary>
/// DTO for pitch detection data coming from audio layer.
/// </summary>
public record DetectedPitchDto
{
    public double FrequencyHz { get; init; }
    public double Confidence { get; init; }
    public double VolumeAmount { get; init; }
    public long TimestampMs { get; init; }
}

/// <summary>
/// DTO for game state during gameplay.
/// </summary>
public record GameStateDto
{
    public string LevelId { get; init; } = string.Empty;
    public string ProfileId { get; init; } = string.Empty;
    public int CurrentScore { get; init; } = 0;
    public int CurrentCombo { get; init; } = 0;
    public int MaxCombo { get; init; } = 0;
    public double CurrentAccuracy { get; init; } = 0;
    public double CurrentTempoMultiplier { get; init; } = 1.0;
    public int Misses { get; init; } = 0;
    public double GameTimeMs { get; init; } = 0;
    public bool IsGameOver { get; init; } = false;
    public bool IsPaused { get; init; } = false;
}

/// <summary>
/// DTO for level preview.
/// </summary>
public record LevelPreviewDto
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Composer { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public int BPM { get; init; }
    public int NoteCount { get; init; }
    public double EstimatedDurationSeconds { get; init; }
    public string PreviewText { get; init; } = string.Empty;
}

/// <summary>
/// DTO for game result summary shown to user.
/// </summary>
public record GameResultSummaryDto
{
    public string LevelTitle { get; init; } = string.Empty;
    public int FinalScore { get; init; }
    public double AccuracyPercent { get; init; }
    public int MaxCombo { get; init; }
    public int PerfectCount { get; init; }
    public int GoodCount { get; init; }
    public int AcceptableCount { get; init; }
    public int MissCount { get; init; }
    public double DurationSeconds { get; init; }
    public double FinalTempoMultiplier { get; init; }
    public bool IsNewBestScore { get; init; }
}

/// <summary>
/// DTO for profile display.
/// </summary>
public record ProfileSummaryDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string SelectedAvatarId { get; init; } = string.Empty;
    public int TotalScore { get; init; }
    public int BestCombo { get; init; }
    public double AverageAccuracy { get; init; }
    public int TotalCompletedLevels { get; init; }
}
