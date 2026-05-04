namespace Neurithm.Application.Services;

using Neurithm.Core.Interfaces;
using Neurithm.Core.Models;

/// <summary>
/// Game engine for scoring and hit evaluation.
/// </summary>
public class GameEngine : IGameEngine
{
    public NoteHitResult EvaluateHit(
        LevelNote expectedNote,
        Note? detectedNote,
        double gameTimeMs,
        GameTimingSettings timingSettings,
        ScoringSettings scoringSettings)
    {
        // Calculate expected time for this note
        var beatsPerMs = 1.0 / (60000.0 / 120.0);  // Will be adjusted by tempo
        var expectedTimeMs = expectedNote.Beat * (60000.0 / 120.0);

        double timingOffsetMs = 0;
        HitResult hitResult;
        var scoreDelta = 0;

        if (detectedNote == null)
        {
            // No note detected - this is a miss
            hitResult = HitResult.Miss;
            scoreDelta = scoringSettings.MissScore;
            timingOffsetMs = double.MaxValue;
        }
        else
        {
            // Calculate timing offset
            timingOffsetMs = detectedNote.TimestampMs - expectedTimeMs;

            // Classify the hit
            hitResult = ClassifyHit(timingOffsetMs, timingSettings);
            scoreDelta = GetScoreDelta(hitResult, scoringSettings);
        }

        return new NoteHitResult
        {
            ExpectedNote = expectedNote,
            DetectedNote = detectedNote,
            TimingOffsetMs = timingOffsetMs,
            Result = hitResult,
            ScoreDelta = scoreDelta,
            GameTimestampMs = (long)gameTimeMs
        };
    }

    public double CalculateAccuracy(IEnumerable<NoteHitResult> results)
    {
        var resultList = results.ToList();
        if (resultList.Count == 0) return 0;

        var successCount = resultList.Count(r => r.Result != HitResult.Miss && r.Result != HitResult.Early && r.Result != HitResult.Late);
        return (successCount / (double)resultList.Count) * 100.0;
    }

    public HitResult ClassifyHit(double timingOffsetMs, GameTimingSettings timingSettings)
    {
        var absoluteOffset = Math.Abs(timingOffsetMs);

        if (absoluteOffset <= timingSettings.PerfectWindowMs)
            return HitResult.Perfect;

        if (absoluteOffset <= timingSettings.GoodWindowMs)
            return HitResult.Good;

        if (absoluteOffset <= timingSettings.AcceptableWindowMs)
            return HitResult.Acceptable;

        // Outside all windows
        return timingOffsetMs < 0 ? HitResult.Early : HitResult.Miss;
    }

    public int GetScoreDelta(HitResult hitResult, ScoringSettings scoringSettings)
    {
        return hitResult switch
        {
            HitResult.Perfect => scoringSettings.PerfectScore,
            HitResult.Good => scoringSettings.GoodScore,
            HitResult.Acceptable => scoringSettings.AcceptableScore,
            HitResult.Miss => scoringSettings.MissScore,
            _ => 0
        };
    }
}

/// <summary>
/// Service for adaptive tempo adjustment.
/// </summary>
public class AdaptiveTempoService : IAdaptiveTempoService
{
    public double CalculateTempoMultiplier(
        IEnumerable<NoteHitResult> recentHits,
        double currentTempoMultiplier,
        AdaptiveTempoSettings settings)
    {
        var recentList = recentHits.OrderByDescending(h => h.GameTimestampMs).Take(settings.AccuracyWindowSize).ToList();

        if (recentList.Count == 0)
            return currentTempoMultiplier;

        // Count hits by result
        var misses = recentList.Count(h => h.Result == HitResult.Miss);
        var successes = recentList.Count(h => h.Result != HitResult.Miss);

        var newTempoMultiplier = currentTempoMultiplier;

        // Apply penalty for misses
        if (misses > 0)
        {
            newTempoMultiplier -= settings.TempoDecreasePerMiss * misses;
        }

        // Apply boost for good accuracy
        if (successes > 0)
        {
            var accuracy = (successes / (double)recentList.Count) * 100.0;
            if (accuracy >= settings.AccuracyThresholdPercent)
            {
                newTempoMultiplier += settings.TempoIncreasePerGood;
            }
        }

        // Clamp to valid range
        return Math.Max(settings.MinTempoMultiplier, Math.Min(settings.MaxTempoMultiplier, newTempoMultiplier));
    }
}

/// <summary>
/// Service for level management.
/// </summary>
public class LevelService : ILevelService
{
    private readonly ILevelRepository _repository;

    public LevelService(ILevelRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<GameLevel>> GetAllLevelsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<GameLevel?> GetLevelAsync(string levelId)
    {
        return await _repository.GetByIdAsync(levelId);
    }

    public async Task<IEnumerable<GameLevel>> GetLevelsByDifficultyAsync(string difficulty)
    {
        return await _repository.GetByDifficultyAsync(difficulty);
    }

    public (bool IsValid, string[] Errors) ValidateLevel(GameLevel level)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(level.Id))
            errors.Add("Level ID is required");

        if (string.IsNullOrEmpty(level.Title))
            errors.Add("Level title is required");

        if (level.BPM <= 0)
            errors.Add("BPM must be greater than 0");

        if (level.Notes.Count == 0)
            errors.Add("Level must have at least one note");

        foreach (var note in level.Notes)
        {
            if (string.IsNullOrEmpty(note.NoteName))
                errors.Add("All notes must have a note name");

            if (note.DurationBeats <= 0)
                errors.Add("Note duration must be greater than 0");
        }

        return (errors.Count == 0, errors.ToArray());
    }
}

/// <summary>
/// Service for profile management.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly IUserProfileRepository _repository;

    public ProfileService(IUserProfileRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<UserProfile>> GetAllProfilesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<UserProfile?> GetProfileAsync(string profileId)
    {
        return await _repository.GetByIdAsync(profileId);
    }

    public async Task<UserProfile> CreateProfileAsync(string displayName, string avatarId = "female_aria")
    {
        var profile = new UserProfile
        {
            Id = Guid.NewGuid().ToString(),
            DisplayName = displayName,
            SelectedAvatarId = avatarId,
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow
        };

        return await _repository.CreateAsync(profile);
    }

    public async Task UpdateProfileAsync(UserProfile profile)
    {
        var updated = profile with { ModifiedUtc = DateTime.UtcNow };
        await _repository.UpdateAsync(updated);
    }

    public async Task DeleteProfileAsync(string profileId)
    {
        await _repository.DeleteAsync(profileId);
    }
}

/// <summary>
/// Service for avatar management.
/// </summary>
public class AvatarService : IAvatarService
{
    private readonly IAvatarRepository _repository;

    public AvatarService(IAvatarRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<Avatar>> GetAllAvatarsAsync()
    {
        var avatars = await _repository.GetAllAsync();
        if (!avatars.Any())
        {
            // Initialize default avatars if none exist
            var defaults = GetDefaultAvatars();
            foreach (var avatar in defaults)
            {
                await _repository.SaveAsync(avatar);
            }
            return defaults;
        }
        return avatars;
    }

    public async Task<Avatar?> GetAvatarAsync(string avatarId)
    {
        return await _repository.GetByIdAsync(avatarId);
    }

    public IEnumerable<Avatar> GetDefaultAvatars()
    {
        return new[]
        {
            new Avatar
            {
                Id = "female_aria",
                Name = "Aria",
                Gender = "Female",
                Description = "Calm classical pianist",
                ImagePath = "/assets/avatars/aria.png",
                IsUnlocked = true,
                ThemeColor = "#4A90E2"
            },
            new Avatar
            {
                Id = "female_mika",
                Name = "Mika",
                Gender = "Female",
                Description = "Energetic rhythm learner",
                ImagePath = "/assets/avatars/mika.png",
                IsUnlocked = true,
                ThemeColor = "#FF6B6B"
            },
            new Avatar
            {
                Id = "female_luna",
                Name = "Luna",
                Gender = "Female",
                Description = "Focused night-practice musician",
                ImagePath = "/assets/avatars/luna.png",
                IsUnlocked = true,
                ThemeColor = "#9B59B6"
            },
            new Avatar
            {
                Id = "male_ren",
                Name = "Ren",
                Gender = "Male",
                Description = "Disciplined concert student",
                ImagePath = "/assets/avatars/ren.png",
                IsUnlocked = true,
                ThemeColor = "#2ECC71"
            },
            new Avatar
            {
                Id = "male_kai",
                Name = "Kai",
                Gender = "Male",
                Description = "Fast reflex rhythm player",
                ImagePath = "/assets/avatars/kai.png",
                IsUnlocked = true,
                ThemeColor = "#E74C3C"
            },
            new Avatar
            {
                Id = "male_noah",
                Name = "Noah",
                Gender = "Male",
                Description = "Relaxed beginner-friendly mentor",
                ImagePath = "/assets/avatars/noah.png",
                IsUnlocked = true,
                ThemeColor = "#F39C12"
            }
        };
    }
}

/// <summary>
/// Service for game result management.
/// </summary>
public class GameResultService : IGameResultService
{
    private readonly IGameResultRepository _repository;
    private readonly ILevelRepository _levelRepository;

    public GameResultService(IGameResultRepository repository, ILevelRepository levelRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _levelRepository = levelRepository ?? throw new ArgumentNullException(nameof(levelRepository));
    }

    public async Task<GameResult> SaveResultAsync(GameResult result)
    {
        return await _repository.SaveAsync(result);
    }

    public async Task<IEnumerable<GameResult>> GetProfileResultsAsync(string profileId)
    {
        return await _repository.GetByProfileAsync(profileId);
    }

    public async Task<GameResult?> GetBestScoreAsync(string profileId, string levelId)
    {
        return await _repository.GetBestScoreAsync(profileId, levelId);
    }

    public async Task<IEnumerable<GameResult>> GetRecentResultsAsync(string profileId, int count = 10)
    {
        var results = await _repository.GetByProfileAsync(profileId);
        return results.OrderByDescending(r => r.CompletedUtc).Take(count);
    }
}
