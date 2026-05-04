namespace Neurithm.Infrastructure.Repositories;

using System.Text.Json;
using System.Text.Json.Serialization;
using Neurithm.Core.Interfaces;
using Neurithm.Core.Models;

/// <summary>
/// File-based implementation of ILevelRepository.
/// Loads levels from JSON files in App_Data/Levels.
/// </summary>
public class FileLevelRepository : ILevelRepository
{
    private readonly string _levelsPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FileLevelRepository(string appDataPath)
    {
        _levelsPath = Path.Combine(appDataPath, "Levels");
        EnsureDirectoryExists(_levelsPath);
    }

    public async Task<IEnumerable<GameLevel>> GetAllAsync()
    {
        var levels = new List<GameLevel>();

        if (!Directory.Exists(_levelsPath))
            return levels;

        var jsonFiles = Directory.GetFiles(_levelsPath, "*.json");
        foreach (var file in jsonFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var level = JsonSerializer.Deserialize<GameLevel>(json, JsonOptions);
                if (level != null)
                    levels.Add(level);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading level from {file}: {ex.Message}");
            }
        }

        return levels.OrderBy(l => l.Difficulty).ThenBy(l => l.Title);
    }

    public async Task<GameLevel?> GetByIdAsync(string levelId)
    {
        var file = Path.Combine(_levelsPath, $"{levelId}.json");
        if (!File.Exists(file))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(file);
            return JsonSerializer.Deserialize<GameLevel>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<GameLevel>> GetByDifficultyAsync(string difficulty)
    {
        var all = await GetAllAsync();
        return all.Where(l => l.Difficulty.Equals(difficulty, StringComparison.OrdinalIgnoreCase));
    }

    public async Task SaveAsync(GameLevel level)
    {
        var file = Path.Combine(_levelsPath, $"{level.Id}.json");
        var json = JsonSerializer.Serialize(level, JsonOptions);
        await File.WriteAllTextAsync(file, json);
    }

    public async Task DeleteAsync(string levelId)
    {
        var file = Path.Combine(_levelsPath, $"{levelId}.json");
        if (File.Exists(file))
            File.Delete(file);
        await Task.CompletedTask;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}

/// <summary>
/// File-based implementation of IUserProfileRepository.
/// Stores profiles as JSON files in App_Data/Profiles.
/// </summary>
public class FileUserProfileRepository : IUserProfileRepository
{
    private readonly string _profilesPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FileUserProfileRepository(string appDataPath)
    {
        _profilesPath = Path.Combine(appDataPath, "Profiles");
        EnsureDirectoryExists(_profilesPath);
    }

    public async Task<IEnumerable<UserProfile>> GetAllAsync()
    {
        var profiles = new List<UserProfile>();

        if (!Directory.Exists(_profilesPath))
            return profiles;

        var jsonFiles = Directory.GetFiles(_profilesPath, "*.json");
        foreach (var file in jsonFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var profile = JsonSerializer.Deserialize<UserProfile>(json, JsonOptions);
                if (profile != null)
                    profiles.Add(profile);
            }
            catch { }
        }

        return profiles.OrderByDescending(p => p.ModifiedUtc);
    }

    public async Task<UserProfile?> GetByIdAsync(string profileId)
    {
        var file = Path.Combine(_profilesPath, $"{profileId}.json");
        if (!File.Exists(file))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(file);
            return JsonSerializer.Deserialize<UserProfile>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<UserProfile> CreateAsync(UserProfile profile)
    {
        await SaveInternalAsync(profile);
        return profile;
    }

    public async Task UpdateAsync(UserProfile profile)
    {
        await SaveInternalAsync(profile);
    }

    public async Task DeleteAsync(string profileId)
    {
        var file = Path.Combine(_profilesPath, $"{profileId}.json");
        if (File.Exists(file))
            File.Delete(file);
        await Task.CompletedTask;
    }

    private async Task SaveInternalAsync(UserProfile profile)
    {
        var file = Path.Combine(_profilesPath, $"{profile.Id}.json");
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        await File.WriteAllTextAsync(file, json);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}

/// <summary>
/// File-based implementation of IAvatarRepository.
/// Stores avatar metadata as JSON in App_Data/Avatars.
/// </summary>
public class FileAvatarRepository : IAvatarRepository
{
    private readonly string _avatarsPath;
    private const string AvatarsMetaFile = "avatars.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FileAvatarRepository(string appDataPath)
    {
        _avatarsPath = Path.Combine(appDataPath, "Avatars");
        EnsureDirectoryExists(_avatarsPath);
    }

    public async Task<IEnumerable<Avatar>> GetAllAsync()
    {
        var metaFile = Path.Combine(_avatarsPath, AvatarsMetaFile);
        if (!File.Exists(metaFile))
            return new List<Avatar>();

        try
        {
            var json = await File.ReadAllTextAsync(metaFile);
            var avatars = JsonSerializer.Deserialize<List<Avatar>>(json, JsonOptions);
            return avatars ?? new List<Avatar>();
        }
        catch
        {
            return new List<Avatar>();
        }
    }

    public async Task<Avatar?> GetByIdAsync(string avatarId)
    {
        var avatars = await GetAllAsync();
        return avatars.FirstOrDefault(a => a.Id == avatarId);
    }

    public async Task SaveAsync(Avatar avatar)
    {
        var avatars = (await GetAllAsync()).ToList();
        var existing = avatars.FirstOrDefault(a => a.Id == avatar.Id);
        if (existing != null)
            avatars.Remove(existing);
        avatars.Add(avatar);

        var metaFile = Path.Combine(_avatarsPath, AvatarsMetaFile);
        var json = JsonSerializer.Serialize(avatars, JsonOptions);
        await File.WriteAllTextAsync(metaFile, json);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}

/// <summary>
/// File-based implementation of IGameResultRepository.
/// Stores results as JSON files in App_Data/GameHistory.
/// </summary>
public class FileGameResultRepository : IGameResultRepository
{
    private readonly string _historyPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FileGameResultRepository(string appDataPath)
    {
        _historyPath = Path.Combine(appDataPath, "GameHistory");
        EnsureDirectoryExists(_historyPath);
    }

    public async Task<GameResult> SaveAsync(GameResult result)
    {
        var file = Path.Combine(_historyPath, $"{result.Id}.json");
        var json = JsonSerializer.Serialize(result, JsonOptions);
        await File.WriteAllTextAsync(file, json);
        return result;
    }

    public async Task<IEnumerable<GameResult>> GetByProfileAsync(string profileId)
    {
        return await GetResultsAsync(r => r.ProfileId == profileId);
    }

    public async Task<IEnumerable<GameResult>> GetByLevelAsync(string levelId)
    {
        return await GetResultsAsync(r => r.LevelId == levelId);
    }

    public async Task<IEnumerable<GameResult>> GetByProfileAndLevelAsync(string profileId, string levelId)
    {
        return await GetResultsAsync(r => r.ProfileId == profileId && r.LevelId == levelId);
    }

    public async Task<GameResult?> GetBestScoreAsync(string profileId, string levelId)
    {
        var results = await GetByProfileAndLevelAsync(profileId, levelId);
        return results.OrderByDescending(r => r.FinalScore).FirstOrDefault();
    }

    private async Task<IEnumerable<GameResult>> GetResultsAsync(Func<GameResult, bool> predicate)
    {
        var results = new List<GameResult>();

        if (!Directory.Exists(_historyPath))
            return results;

        var jsonFiles = Directory.GetFiles(_historyPath, "*.json");
        foreach (var file in jsonFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var result = JsonSerializer.Deserialize<GameResult>(json, JsonOptions);
                if (result != null && predicate(result))
                    results.Add(result);
            }
            catch { }
        }

        return results.OrderByDescending(r => r.CompletedUtc);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}

/// <summary>
/// File-based implementation of ISettingsRepository.
/// Loads settings from JSON files in App_Data/Settings.
/// </summary>
public class FileSettingsRepository : ISettingsRepository
{
    private readonly string _settingsPath;
    private const string AudioSettingsFile = "audio.json";
    private const string TimingSettingsFile = "timing.json";
    private const string AdaptiveTempoSettingsFile = "adaptiveTempo.json";
    private const string ScoringSettingsFile = "scoring.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public FileSettingsRepository(string appDataPath)
    {
        _settingsPath = Path.Combine(appDataPath, "Settings");
        EnsureDirectoryExists(_settingsPath);
        InitializeDefaultSettings();
    }

    public async Task<AudioCalibrationSettings> GetAudioSettingsAsync()
    {
        return await LoadOrCreateAsync(AudioSettingsFile, new AudioCalibrationSettings());
    }

    public async Task SaveAudioSettingsAsync(AudioCalibrationSettings settings)
    {
        await SaveAsync(AudioSettingsFile, settings);
    }

    public async Task<GameTimingSettings> GetTimingSettingsAsync()
    {
        return await LoadOrCreateAsync(TimingSettingsFile, new GameTimingSettings());
    }

    public async Task<AdaptiveTempoSettings> GetAdaptiveTempoSettingsAsync()
    {
        return await LoadOrCreateAsync(AdaptiveTempoSettingsFile, new AdaptiveTempoSettings());
    }

    public async Task<ScoringSettings> GetScoringSettingsAsync()
    {
        return await LoadOrCreateAsync(ScoringSettingsFile, new ScoringSettings());
    }

    private async Task<T> LoadOrCreateAsync<T>(string fileName, T defaultSettings) where T : new()
    {
        var file = Path.Combine(_settingsPath, fileName);
        if (File.Exists(file))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var loaded = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return loaded ?? defaultSettings;
            }
            catch
            {
                return defaultSettings;
            }
        }

        // Create with defaults
        await SaveAsync(fileName, defaultSettings);
        return defaultSettings;
    }

    private async Task SaveAsync<T>(string fileName, T settings)
    {
        var file = Path.Combine(_settingsPath, fileName);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(file, json);
    }

    private void InitializeDefaultSettings()
    {
        // Ensure default files exist
        var audioFile = Path.Combine(_settingsPath, AudioSettingsFile);
        if (!File.Exists(audioFile))
        {
            var json = JsonSerializer.Serialize(new AudioCalibrationSettings(), JsonOptions);
            File.WriteAllText(audioFile, json);
        }

        var timingFile = Path.Combine(_settingsPath, TimingSettingsFile);
        if (!File.Exists(timingFile))
        {
            var json = JsonSerializer.Serialize(new GameTimingSettings(), JsonOptions);
            File.WriteAllText(timingFile, json);
        }

        var tempoFile = Path.Combine(_settingsPath, AdaptiveTempoSettingsFile);
        if (!File.Exists(tempoFile))
        {
            var json = JsonSerializer.Serialize(new AdaptiveTempoSettings(), JsonOptions);
            File.WriteAllText(tempoFile, json);
        }

        var scoringFile = Path.Combine(_settingsPath, ScoringSettingsFile);
        if (!File.Exists(scoringFile))
        {
            var json = JsonSerializer.Serialize(new ScoringSettings(), JsonOptions);
            File.WriteAllText(scoringFile, json);
        }
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
