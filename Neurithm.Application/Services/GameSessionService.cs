using Neurithm.Audio;
using Neurithm.Core.Models;

namespace Neurithm.Application.Services;

public sealed class GameSessionService : IGameSessionService
{
    private const double PerfectWindowMs = 50.0;
    private const double GoodWindowMs = 100.0;
    private const double AcceptableWindowMs = 150.0;

    private readonly IHitDetectionService _hitDetectionService;
    private readonly IAdaptiveTempoService _adaptiveTempoService;

    private readonly List<HitResult> _recentHits = [];

    private Level? _level;
    private bool _isLoaded;
    private bool _isRunning;
    private bool _isCountdown;
    private bool _isCompleted;
    private bool _isPaused;

    private int _countdownValue;
    private double _countdownRemainingMs;

    private int _score;
    private int _combo;
    private int _maxCombo;
    private int _misses;
    private int _perfectCount;
    private int _goodCount;
    private int _acceptableCount;

    private double _tempoMultiplier = 1.0;
    private string _timingFeedback = "--";
    private string _currentDetectedNote = "--";

    private double _elapsedSongMs;
    private double _lastRawElapsedMs;

    private readonly List<LevelNoteState> _states = [];

    public GameSessionService(IHitDetectionService hitDetectionService, IAdaptiveTempoService adaptiveTempoService)
    {
        _hitDetectionService = hitDetectionService;
        _adaptiveTempoService = adaptiveTempoService;
    }

    public void LoadLevel(Level level)
    {
        Reset();

        _level = level;
        _isLoaded = true;

        _states.Clear();
        var ordered = level.Notes.OrderBy(n => n.Beat).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            var note = ordered[i];
            var startMs = BeatToMilliseconds(note.Beat, level.Bpm);
            var durationMs = BeatToMilliseconds(note.DurationBeats, level.Bpm);
            _states.Add(new LevelNoteState(note, $"{note.Note}_{i}", startMs, durationMs, LaneIndexFromNoteName(note.Note)));
        }
    }

    public void StartCountdown(int seconds = 3)
    {
        if (!_isLoaded || _level is null)
        {
            return;
        }

        _isCountdown = true;
        _isRunning = false;
        _isCompleted = false;
        _countdownValue = Math.Clamp(seconds, 1, 9);
        _countdownRemainingMs = _countdownValue * 1000.0;
        _elapsedSongMs = 0.0;
        _lastRawElapsedMs = 0.0;
    }

    public void Pause()
    {
        if (!_isRunning || _isCompleted || _isCountdown)
        {
            return;
        }

        _isPaused = true;
        _isRunning = false;
        _timingFeedback = "Paused";
    }

    public void Resume()
    {
        if (!_isPaused || _isCompleted)
        {
            return;
        }

        _isPaused = false;
        _isRunning = true;
        _timingFeedback = "Playing";
    }

    public void Update(TimeSpan elapsed)
    {
        if (!_isLoaded || _level is null)
        {
            return;
        }

        var rawElapsedMs = elapsed.TotalMilliseconds;
        var deltaRawMs = Math.Max(0.0, rawElapsedMs - _lastRawElapsedMs);
        _lastRawElapsedMs = rawElapsedMs;

        if (_isCountdown)
        {
            _countdownRemainingMs = Math.Max(0.0, _countdownRemainingMs - deltaRawMs);
            _countdownValue = (int)Math.Ceiling(_countdownRemainingMs / 1000.0);

            if (_countdownRemainingMs <= 0.0)
            {
                _isCountdown = false;
                _isPaused = false;
                _isRunning = true;
                _countdownValue = 0;
                _timingFeedback = "Start";
            }

            return;
        }

        if (_isPaused || !_isRunning || _isCompleted)
        {
            return;
        }

        _elapsedSongMs += deltaRawMs * _tempoMultiplier;

        foreach (var state in _states)
        {
            if (state.IsResolved)
            {
                continue;
            }

            var offset = _elapsedSongMs - state.StartTimeMs;
            if (offset > AcceptableWindowMs)
            {
                state.ResolveAsMiss("Miss");
                ApplyHitResult(new HitResult("Miss", offset, -1, false));
            }
        }

        if (_states.All(s => s.IsResolved))
        {
            _isCompleted = true;
            _isRunning = false;
        }
    }

    public void RegisterDetection(DetectedNoteResult detection)
    {
        _currentDetectedNote = detection.NoteName;

        if (!_isRunning || _isCountdown || _isCompleted || _level is null || !detection.IsReliable)
        {
            return;
        }

        var candidates = _states
            .Where(s => !s.IsResolved && string.Equals(s.Note.Note, detection.NoteName, StringComparison.OrdinalIgnoreCase))
            .Select(s => new { State = s, Offset = _elapsedSongMs - s.StartTimeMs })
            .OrderBy(x => Math.Abs(x.Offset))
            .ToList();

        var best = candidates.FirstOrDefault(x => Math.Abs(x.Offset) <= AcceptableWindowMs);
        if (best is null)
        {
            return;
        }

        var expected = ToNote(best.State.Note.Note);
        var actual = new Note(detection.NoteName, NoteNameToMidi(detection.NoteName), detection.FrequencyHz);
        var hit = _hitDetectionService.DetectHit(expected, actual, best.Offset);

        if (!hit.IsSuccessfulHit)
        {
            return;
        }

        var feedback = hit.Type;
        if (hit.Type != "Miss")
        {
            feedback = best.Offset < 0 ? $"{hit.Type} Early" : best.Offset > 0 ? $"{hit.Type} Late" : hit.Type;
        }

        best.State.Resolve(feedback);
        ApplyHitResult(hit);
    }

    public GameSessionSnapshot GetSnapshot()
    {
        var target = _states.FirstOrDefault(s => !s.IsResolved)?.Note.Note ?? "--";
        var visuals = BuildVisuals();

        var totalResolved = _perfectCount + _goodCount + _acceptableCount + _misses;
        var successfulHits = _perfectCount + _goodCount + _acceptableCount;
        var accuracy = totalResolved == 0 ? 0.0 : (successfulHits * 100.0) / totalResolved;

        return new GameSessionSnapshot(
            IsLoaded: _isLoaded,
            IsRunning: _isRunning,
            IsPaused: _isPaused,
            IsCountdown: _isCountdown,
            IsCompleted: _isCompleted,
            LevelId: _level?.Id ?? "--",
            LevelTitle: _level?.Title ?? "--",
            CountdownValue: _countdownValue,
            Score: _score,
            Combo: _combo,
            MaxCombo: _maxCombo,
            Misses: _misses,
            PerfectCount: _perfectCount,
            GoodCount: _goodCount,
            AcceptableCount: _acceptableCount,
            TotalResolved: totalResolved,
            Accuracy: accuracy,
            TempoMultiplier: _tempoMultiplier,
            CurrentDetectedNote: _currentDetectedNote,
            CurrentTargetNote: target,
            TimingFeedback: _timingFeedback,
            ElapsedSongMilliseconds: _elapsedSongMs,
            FallingNotes: visuals);
    }

    public void Reset()
    {
        _isLoaded = false;
        _isRunning = false;
        _isPaused = false;
        _isCountdown = false;
        _isCompleted = false;

        _countdownValue = 0;
        _countdownRemainingMs = 0.0;

        _score = 0;
        _combo = 0;
        _maxCombo = 0;
        _misses = 0;
        _perfectCount = 0;
        _goodCount = 0;
        _acceptableCount = 0;

        _tempoMultiplier = 1.0;
        _timingFeedback = "--";
        _currentDetectedNote = "--";

        _elapsedSongMs = 0.0;
        _lastRawElapsedMs = 0.0;

        _states.Clear();
        _recentHits.Clear();
    }

    private IReadOnlyList<FallingNoteVisual> BuildVisuals()
    {
        if (_level is null)
        {
            return [];
        }

        const double approachMs = 3000.0;
        var notes = new List<FallingNoteVisual>(_states.Count);

        foreach (var state in _states)
        {
            var offsetMs = state.StartTimeMs - _elapsedSongMs;
            var progress = (approachMs - offsetMs) / approachMs;
            var top = progress * 100.0;
            top = Math.Clamp(top, -12.0, 120.0);

            var durationRatio = state.DurationMs / approachMs;
            var height = Math.Clamp(durationRatio * 100.0, 4.0, 24.0);

            var insideHitWindow = Math.Abs(_elapsedSongMs - state.StartTimeMs) <= AcceptableWindowMs;

            notes.Add(new FallingNoteVisual(
                NoteId: state.NoteId,
                NoteName: state.Note.Note,
                LaneIndex: state.LaneIndex,
                TopPercent: top,
                HeightPercent: height,
                IsInsideHitWindow: insideHitWindow,
                IsResolved: state.IsResolved,
                IsMissed: state.IsMissed,
                Feedback: state.Feedback));
        }

        return notes;
    }

    private void ApplyHitResult(HitResult hit)
    {
        _recentHits.Add(hit);
        if (_recentHits.Count > 64)
        {
            _recentHits.RemoveRange(0, _recentHits.Count - 64);
        }

        _tempoMultiplier = _adaptiveTempoService.CalculateTempoMultiplier(_tempoMultiplier, _recentHits);
        _timingFeedback = hit.Type;
        _score += hit.ScoreDelta;

        if (hit.IsSuccessfulHit)
        {
            _combo++;
            _maxCombo = Math.Max(_maxCombo, _combo);

            if (hit.Type == "Perfect")
            {
                _perfectCount++;
            }
            else if (hit.Type == "Good")
            {
                _goodCount++;
            }
            else if (hit.Type == "Acceptable")
            {
                _acceptableCount++;
            }
        }
        else
        {
            _misses++;
            _combo = 0;
        }
    }

    private static double BeatToMilliseconds(double beat, int bpm)
    {
        var beatsPerSecond = bpm / 60.0;
        return (beat / beatsPerSecond) * 1000.0;
    }

    private static int LaneIndexFromNoteName(string note)
    {
        var midi = NoteNameToMidi(note);
        return Math.Clamp(midi - 36, 0, 60);
    }

    private static Note ToNote(string noteName)
    {
        var midi = NoteNameToMidi(noteName);
        var frequency = 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);
        return new Note(noteName, midi, frequency);
    }

    private static int NoteNameToMidi(string note)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["C"] = 0,
            ["C#"] = 1,
            ["D"] = 2,
            ["D#"] = 3,
            ["E"] = 4,
            ["F"] = 5,
            ["F#"] = 6,
            ["G"] = 7,
            ["G#"] = 8,
            ["A"] = 9,
            ["A#"] = 10,
            ["B"] = 11
        };

        if (string.IsNullOrWhiteSpace(note) || note.Length < 2)
        {
            return 69;
        }

        var pitch = note.Length >= 3 && note[1] == '#'
            ? note[..2]
            : note[..1];

        var octaveText = note[pitch.Length..];
        if (!int.TryParse(octaveText, out var octave))
        {
            octave = 4;
        }

        if (!map.TryGetValue(pitch, out var semitone))
        {
            semitone = 9;
        }

        return (octave + 1) * 12 + semitone;
    }

    private sealed class LevelNoteState
    {
        public LevelNoteState(LevelNote note, string noteId, double startTimeMs, double durationMs, int laneIndex)
        {
            Note = note;
            NoteId = noteId;
            StartTimeMs = startTimeMs;
            DurationMs = durationMs;
            LaneIndex = laneIndex;
        }

        public LevelNote Note { get; }
        public string NoteId { get; }
        public double StartTimeMs { get; }
        public double DurationMs { get; }
        public int LaneIndex { get; }

        public bool IsResolved { get; private set; }
        public bool IsMissed { get; private set; }
        public string Feedback { get; private set; } = "--";

        public void Resolve(string feedback)
        {
            IsResolved = true;
            IsMissed = false;
            Feedback = feedback;
        }

        public void ResolveAsMiss(string feedback)
        {
            IsResolved = true;
            IsMissed = true;
            Feedback = feedback;
        }
    }
}
