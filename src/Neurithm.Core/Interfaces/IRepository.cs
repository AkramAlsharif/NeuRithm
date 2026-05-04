namespace Neurithm.Core.Interfaces;

using Neurithm.Core.Models;

/// <summary>
/// Interface for level repository - abstraction over storage.
/// </summary>
public interface ILevelRepository
{
    /// <summary>
    /// Get all available levels.
    /// </summary>
    Task<IEnumerable<GameLevel>> GetAllAsync();

    /// <summary>
    /// Get a specific level by ID.
    /// </summary>
    Task<GameLevel?> GetByIdAsync(string levelId);

    /// <summary>
    /// Get levels by difficulty.
    /// </summary>
    Task<IEnumerable<GameLevel>> GetByDifficultyAsync(string difficulty);

    /// <summary>
    /// Save a level.
    /// </summary>
    Task SaveAsync(GameLevel level);

    /// <summary>
    /// Delete a level.
    /// </summary>
    Task DeleteAsync(string levelId);
}

/// <summary>
/// Interface for user profile repository.
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>
    /// Get all profiles.
    /// </summary>
    Task<IEnumerable<UserProfile>> GetAllAsync();

    /// <summary>
    /// Get a specific profile by ID.
    /// </summary>
    Task<UserProfile?> GetByIdAsync(string profileId);

    /// <summary>
    /// Create a new profile.
    /// </summary>
    Task<UserProfile> CreateAsync(UserProfile profile);

    /// <summary>
    /// Update an existing profile.
    /// </summary>
    Task UpdateAsync(UserProfile profile);

    /// <summary>
    /// Delete a profile.
    /// </summary>
    Task DeleteAsync(string profileId);
}

/// <summary>
/// Interface for avatar repository.
/// </summary>
public interface IAvatarRepository
{
    /// <summary>
    /// Get all available avatars.
    /// </summary>
    Task<IEnumerable<Avatar>> GetAllAsync();

    /// <summary>
    /// Get a specific avatar by ID.
    /// </summary>
    Task<Avatar?> GetByIdAsync(string avatarId);

    /// <summary>
    /// Save avatar metadata.
    /// </summary>
    Task SaveAsync(Avatar avatar);
}

/// <summary>
/// Interface for game result/history repository.
/// </summary>
public interface IGameResultRepository
{
    /// <summary>
    /// Save a game result.
    /// </summary>
    Task<GameResult> SaveAsync(GameResult result);

    /// <summary>
    /// Get results for a specific profile.
    /// </summary>
    Task<IEnumerable<GameResult>> GetByProfileAsync(string profileId);

    /// <summary>
    /// Get results for a specific level.
    /// </summary>
    Task<IEnumerable<GameResult>> GetByLevelAsync(string levelId);

    /// <summary>
    /// Get results for a profile and level combination.
    /// </summary>
    Task<IEnumerable<GameResult>> GetByProfileAndLevelAsync(string profileId, string levelId);

    /// <summary>
    /// Get best score for a level by a profile.
    /// </summary>
    Task<GameResult?> GetBestScoreAsync(string profileId, string levelId);
}

/// <summary>
/// Interface for application settings repository.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Get audio calibration settings.
    /// </summary>
    Task<AudioCalibrationSettings> GetAudioSettingsAsync();

    /// <summary>
    /// Save audio calibration settings.
    /// </summary>
    Task SaveAudioSettingsAsync(AudioCalibrationSettings settings);

    /// <summary>
    /// Get game timing settings.
    /// </summary>
    Task<GameTimingSettings> GetTimingSettingsAsync();

    /// <summary>
    /// Get adaptive tempo settings.
    /// </summary>
    Task<AdaptiveTempoSettings> GetAdaptiveTempoSettingsAsync();

    /// <summary>
    /// Get scoring settings.
    /// </summary>
    Task<ScoringSettings> GetScoringSettingsAsync();
}
