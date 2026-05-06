using System.Collections.Generic;
using System.Threading.Tasks;
using Neurithm.Audio;
using Neurithm.Core.Models;

namespace Neurithm.Application.Services
{
    public interface ILevelService
    {
        Task<IReadOnlyList<Level>> GetAllLevelsAsync();
        Task<Level?> GetLevelByIdAsync(string id);
    }

    public interface IProfileService
    {
        Task<Profile?> GetProfileAsync(string displayName);
        Task SaveProfileAsync(Profile profile);
    }

    public interface IAvatarService
    {
        Task<IReadOnlyList<Avatar>> GetAllAvatarsAsync();
        Task<Avatar?> GetAvatarByIdAsync(string id);
    }

    public interface IScoreService
    {
        Task SaveScoreAsync(Score score);
        Task<IReadOnlyList<Score>> GetScoresByProfileAsync(string profileId);
    }

    public interface IHitDetectionService
    {
        HitResult DetectHit(Note expected, Note actual, double offsetMs);
    }

    public interface IAdaptiveTempoService
    {
        double CalculateTempoMultiplier(double currentTempo, IReadOnlyList<HitResult> recentHits);
    }

    public interface ICalibrationService
    {
        Task<double> CalibrateLatencyAsync();
        Task<double> CalibrateA4Async();
        CalibrationSettings GetCurrentSettings();
        Task UpdateAsync(CalibrationSettings settings);
    }

    public interface IGameSessionService
    {
        void LoadLevel(Level level);
        void StartCountdown(int seconds = 3);
        void Update(TimeSpan elapsed);
        void RegisterDetection(DetectedNoteResult detection);
        void Pause();
        void Resume();
        GameSessionSnapshot GetSnapshot();
        void Reset();
    }

    public sealed record FallingNoteVisual(
        string NoteId,
        string NoteName,
        int LaneIndex,
        double TopPercent,
        double HeightPercent,
        bool IsInsideHitWindow,
        bool IsResolved,
        bool IsMissed,
        string Feedback);

    public sealed record GameSessionSnapshot(
        bool IsLoaded,
        bool IsRunning,
        bool IsPaused,
        bool IsCountdown,
        bool IsCompleted,
        string LevelId,
        string LevelTitle,
        int CountdownValue,
        int Score,
        int Combo,
        int MaxCombo,
        int Misses,
        int PerfectCount,
        int GoodCount,
        int AcceptableCount,
        int TotalResolved,
        double Accuracy,
        double TempoMultiplier,
        string CurrentDetectedNote,
        string CurrentTargetNote,
        string TimingFeedback,
        double ElapsedSongMilliseconds,
        IReadOnlyList<FallingNoteVisual> FallingNotes);
}
