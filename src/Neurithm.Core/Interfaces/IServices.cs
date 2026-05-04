namespace Neurithm.Core.Interfaces;

using Neurithm.Core.Models;

/// <summary>
/// Service for pitch detection and audio analysis.
/// Converts frequencies to notes.
/// </summary>
public interface IPitchDetectionService
{
    /// <summary>
    /// Convert frequency (Hz) to a musical note.
    /// </summary>
    Note FrequencyToNote(double frequencyHz, double volumeAmount, long timestampMs);

    /// <summary>
    /// Convert note name to frequency.
    /// </summary>
    double NoteToFrequency(string noteName);

    /// <summary>
    /// Parse note name (e.g., "C4") into note number and octave.
    /// </summary>
    (char Note, int Octave) ParseNoteName(string noteName);

    /// <summary>
    /// Get note name from MIDI number.
    /// </summary>
    string MidiNumberToNoteName(int midiNumber);

    /// <summary>
    /// Get MIDI number from note name.
    /// </summary>
    int NoteNameToMidiNumber(string noteName);
}

/// <summary>
/// Service for game logic and scoring.
/// </summary>
public interface IGameEngine
{
    /// <summary>
    /// Evaluate a detected note against an expected note.
    /// </summary>
    NoteHitResult EvaluateHit(LevelNote expectedNote, Note? detectedNote, double gameTimeMs, GameTimingSettings timingSettings, ScoringSettings scoringSettings);

    /// <summary>
    /// Calculate current accuracy percentage.
    /// </summary>
    double CalculateAccuracy(IEnumerable<NoteHitResult> results);

    /// <summary>
    /// Determine the hit result classification.
    /// </summary>
    HitResult ClassifyHit(double timingOffsetMs, GameTimingSettings timingSettings);

    /// <summary>
    /// Get score delta for a hit result.
    /// </summary>
    int GetScoreDelta(HitResult hitResult, ScoringSettings scoringSettings);
}

/// <summary>
/// Service for adaptive tempo calculation.
/// </summary>
public interface IAdaptiveTempoService
{
    /// <summary>
    /// Calculate new tempo multiplier based on recent performance.
    /// </summary>
    double CalculateTempoMultiplier(
        IEnumerable<NoteHitResult> recentHits,
        double currentTempoMultiplier,
        AdaptiveTempoSettings settings);
}

/// <summary>
/// Service for level management and validation.
/// </summary>
public interface ILevelService
{
    /// <summary>
    /// Get all available levels.
    /// </summary>
    Task<IEnumerable<GameLevel>> GetAllLevelsAsync();

    /// <summary>
    /// Get a specific level.
    /// </summary>
    Task<GameLevel?> GetLevelAsync(string levelId);

    /// <summary>
    /// Get levels by difficulty.
    /// </summary>
    Task<IEnumerable<GameLevel>> GetLevelsByDifficultyAsync(string difficulty);

    /// <summary>
    /// Validate a level for gameplay.
    /// </summary>
    (bool IsValid, string[] Errors) ValidateLevel(GameLevel level);
}

/// <summary>
/// Service for profile management.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Get all user profiles.
    /// </summary>
    Task<IEnumerable<UserProfile>> GetAllProfilesAsync();

    /// <summary>
    /// Get a specific profile.
    /// </summary>
    Task<UserProfile?> GetProfileAsync(string profileId);

    /// <summary>
    /// Create a new profile.
    /// </summary>
    Task<UserProfile> CreateProfileAsync(string displayName, string avatarId = "female_aria");

    /// <summary>
    /// Update a profile.
    /// </summary>
    Task UpdateProfileAsync(UserProfile profile);

    /// <summary>
    /// Delete a profile.
    /// </summary>
    Task DeleteProfileAsync(string profileId);
}

/// <summary>
/// Service for avatar management.
/// </summary>
public interface IAvatarService
{
    /// <summary>
    /// Get all available avatars.
    /// </summary>
    Task<IEnumerable<Avatar>> GetAllAvatarsAsync();

    /// <summary>
    /// Get a specific avatar.
    /// </summary>
    Task<Avatar?> GetAvatarAsync(string avatarId);

    /// <summary>
    /// Get default avatars for initialization.
    /// </summary>
    IEnumerable<Avatar> GetDefaultAvatars();
}

/// <summary>
/// Service for game result recording and retrieval.
/// </summary>
public interface IGameResultService
{
    /// <summary>
    /// Save a game result.
    /// </summary>
    Task<GameResult> SaveResultAsync(GameResult result);

    /// <summary>
    /// Get results for a profile.
    /// </summary>
    Task<IEnumerable<GameResult>> GetProfileResultsAsync(string profileId);

    /// <summary>
    /// Get best score for a level.
    /// </summary>
    Task<GameResult?> GetBestScoreAsync(string profileId, string levelId);

    /// <summary>
    /// Get recent results (last N results).
    /// </summary>
    Task<IEnumerable<GameResult>> GetRecentResultsAsync(string profileId, int count = 10);
}
