using System;

namespace Neurithm.Audio;

public sealed class SimplePitchDetector : IPitchDetector
{
    private static readonly string[] NoteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    private readonly Queue<DetectionSample> _recentSamples = new();

    private PitchDetectorOptions _options;

    private string? _lastCandidateNote;
    private int _stableCount;
    private DateTime _lastReliableAtUtc;

    public SimplePitchDetector(PitchDetectorOptions? options = null)
    {
        _options = options ?? new PitchDetectorOptions();
    }

    public PitchDetectorOptions GetOptions()
        => new()
        {
            A4FrequencyHz = _options.A4FrequencyHz,
            MinFrequencyHz = _options.MinFrequencyHz,
            MaxFrequencyHz = _options.MaxFrequencyHz,
            MinRmsForDetection = _options.MinRmsForDetection,
            MinConfidence = _options.MinConfidence,
            MinStableDetections = _options.MinStableDetections,
            DebounceMilliseconds = _options.DebounceMilliseconds,
            LowPassCutoffHz = _options.LowPassCutoffHz,
            PianoRangeMarginSemitones = _options.PianoRangeMarginSemitones,
            MaxCentsFromNearestNote = _options.MaxCentsFromNearestNote,
            MinCorrelationPeak = _options.MinCorrelationPeak,
            SmoothingWindowMilliseconds = _options.SmoothingWindowMilliseconds,
            FrameIntervalMilliseconds = _options.FrameIntervalMilliseconds,
            MinSamplesForAveraging = _options.MinSamplesForAveraging,
            DominantNoteVoteThreshold = _options.DominantNoteVoteThreshold
        };

    public void UpdateOptions(PitchDetectorOptions options)
    {
        _options = new PitchDetectorOptions
        {
            A4FrequencyHz = options.A4FrequencyHz,
            MinFrequencyHz = options.MinFrequencyHz,
            MaxFrequencyHz = options.MaxFrequencyHz,
            MinRmsForDetection = options.MinRmsForDetection,
            MinConfidence = options.MinConfidence,
            MinStableDetections = options.MinStableDetections,
            DebounceMilliseconds = options.DebounceMilliseconds,
            LowPassCutoffHz = options.LowPassCutoffHz,
            PianoRangeMarginSemitones = options.PianoRangeMarginSemitones,
            MaxCentsFromNearestNote = options.MaxCentsFromNearestNote,
            MinCorrelationPeak = options.MinCorrelationPeak,
            SmoothingWindowMilliseconds = options.SmoothingWindowMilliseconds,
            FrameIntervalMilliseconds = options.FrameIntervalMilliseconds,
            MinSamplesForAveraging = options.MinSamplesForAveraging,
            DominantNoteVoteThreshold = options.DominantNoteVoteThreshold
        };

        ResetStability();
        _recentSamples.Clear();
    }

    public DetectedNoteResult AnalyzeFrame(float[] audioBuffer, int sampleRate)
    {
        var detectedAtUtc = DateTime.UtcNow;

        if (audioBuffer is null || audioBuffer.Length < 256 || sampleRate <= 0)
        {
            return CreateUnreliable("--", 0.0, 0.0, 0.0, 0.0, detectedAtUtc);
        }

        var rms = ComputeRms(audioBuffer);
        if (rms < _options.MinRmsForDetection)
        {
            ResetStability();
            return CreateUnreliable("--", 0.0, 0.0, 0.0, rms, detectedAtUtc);
        }

        var conditioned = PrepareSignal(audioBuffer);
        ApplyOnePoleLowPassInPlace(conditioned, sampleRate, _options.LowPassCutoffHz);

        var minLag = Math.Max(1, (int)Math.Floor(sampleRate / _options.MaxFrequencyHz));
        var maxLag = Math.Min(conditioned.Length / 2, (int)Math.Ceiling(sampleRate / _options.MinFrequencyHz));

        if (maxLag <= minLag)
        {
            ResetStability();
            return CreateUnreliable("--", 0.0, 0.0, 0.0, rms, detectedAtUtc);
        }

        var normalized = BuildNormalizedAutocorrelation(conditioned, minLag, maxLag);
        var bestLag = -1;
        var bestScore = 0.0;

        for (var lag = minLag; lag <= maxLag; lag++)
        {
            var idx = lag - minLag;
            var score = normalized[idx];
            if (score > bestScore)
            {
                bestScore = score;
                bestLag = lag;
            }
        }

        if (bestLag <= 0 || bestScore < _options.MinCorrelationPeak)
        {
            ResetStability();
            return CreateUnreliable("--", 0.0, 0.0, 0.0, rms, detectedAtUtc);
        }

        var correctedLag = PreferFundamentalLag(bestLag, minLag, maxLag, normalized, bestScore);
        var refinedLag = ParabolicInterpolateLag(correctedLag, minLag, normalized);
        var frequencyHz = sampleRate / refinedLag;

        if (frequencyHz < _options.MinFrequencyHz || frequencyHz > _options.MaxFrequencyHz)
        {
            ResetStability();
            return CreateUnreliable("--", frequencyHz, bestScore, 0.0, rms, detectedAtUtc);
        }

        var noteNumber = FrequencyToMidiNoteNumber(frequencyHz, _options.A4FrequencyHz);
        var nearestMidi = (int)Math.Round(noteNumber);
        var centsFromNearest = 100.0 * (noteNumber - nearestMidi);

        if (!IsWithinPianoRange(nearestMidi, _options.PianoRangeMarginSemitones)
            || Math.Abs(centsFromNearest) > _options.MaxCentsFromNearestNote)
        {
            ResetStability();
            return CreateUnreliable("--", frequencyHz, bestScore, centsFromNearest, rms, detectedAtUtc);
        }

        var correlationScore = normalized[correctedLag - minLag];
        var confidence = Clamp01(correlationScore);
        var noteName = MidiToNoteName(nearestMidi);

        AddRecentSample(new DetectionSample(detectedAtUtc, frequencyHz, confidence, noteName, centsFromNearest));
        TrimSamples(detectedAtUtc);

        if (!TryBuildBoundedMaterialization(out var materialized))
        {
            return CreateUnreliable(noteName, frequencyHz, confidence, centsFromNearest, rms, detectedAtUtc);
        }

        var isReliable = materialized.Confidence >= _options.MinConfidence
            && UpdateStability(materialized.NoteName, detectedAtUtc);

        return new DetectedNoteResult(
            materialized.NoteName,
            materialized.FrequencyHz,
            materialized.Confidence,
            materialized.CentsOffset,
            rms,
            detectedAtUtc,
            isReliable);
    }

    private void AddRecentSample(DetectionSample sample)
        => _recentSamples.Enqueue(sample);

    private void TrimSamples(DateTime detectedAtUtc)
    {
        var windowMs = Math.Max(80, _options.SmoothingWindowMilliseconds);
        while (_recentSamples.Count > 0
               && (detectedAtUtc - _recentSamples.Peek().DetectedAtUtc).TotalMilliseconds > windowMs)
        {
            _recentSamples.Dequeue();
        }
    }

    private bool TryBuildBoundedMaterialization(out DetectionSample materialized)
    {
        materialized = default;

        if (_recentSamples.Count < Math.Max(2, _options.MinSamplesForAveraging))
        {
            return false;
        }

        var grouped = _recentSamples
            .GroupBy(s => s.NoteName)
            .Select(g => new
            {
                Note = g.Key,
                Count = g.Count(),
                AverageFrequency = g.Average(x => x.FrequencyHz),
                AverageConfidence = g.Average(x => x.Confidence),
                AverageCents = g.Average(x => x.CentsOffset),
                LastAt = g.Max(x => x.DetectedAtUtc)
            })
            .OrderByDescending(g => g.Count)
            .ThenByDescending(g => g.AverageConfidence)
            .FirstOrDefault();

        if (grouped is null)
        {
            return false;
        }

        var voteRatio = grouped.Count / (double)_recentSamples.Count;
        if (voteRatio < _options.DominantNoteVoteThreshold)
        {
            return false;
        }

        materialized = new DetectionSample(
            grouped.LastAt,
            grouped.AverageFrequency,
            grouped.AverageConfidence,
            grouped.Note,
            grouped.AverageCents);

        return true;
    }

    private static double[] BuildNormalizedAutocorrelation(float[] buffer, int minLag, int maxLag)
    {
        var length = buffer.Length;
        var energy = 0.0;

        for (var i = 0; i < length; i++)
        {
            var value = buffer[i];
            energy += value * value;
        }

        if (energy <= 0)
        {
            return new double[maxLag - minLag + 1];
        }

        var result = new double[maxLag - minLag + 1];
        for (var lag = minLag; lag <= maxLag; lag++)
        {
            var sum = 0.0;
            var max = length - lag;
            for (var i = 0; i < max; i++)
            {
                sum += buffer[i] * buffer[i + lag];
            }

            result[lag - minLag] = sum / energy;
        }

        return result;
    }

    private static double ParabolicInterpolateLag(int bestLag, int minLag, double[] values)
    {
        var idx = bestLag - minLag;
        if (idx <= 0 || idx >= values.Length - 1)
        {
            return bestLag;
        }

        var left = values[idx - 1];
        var center = values[idx];
        var right = values[idx + 1];

        var denominator = (left - (2 * center) + right);
        if (Math.Abs(denominator) < 1e-10)
        {
            return bestLag;
        }

        var shift = 0.5 * (left - right) / denominator;
        return bestLag + shift;
    }

    private bool UpdateStability(string noteName, DateTime detectedAtUtc)
    {
        if (string.Equals(noteName, _lastCandidateNote, StringComparison.OrdinalIgnoreCase))
        {
            _stableCount++;
        }
        else
        {
            _lastCandidateNote = noteName;
            _stableCount = 1;
        }

        if (_stableCount < _options.MinStableDetections)
        {
            return false;
        }

        if ((detectedAtUtc - _lastReliableAtUtc).TotalMilliseconds < _options.DebounceMilliseconds)
        {
            return false;
        }

        _lastReliableAtUtc = detectedAtUtc;
        return true;
    }

    private void ResetStability()
    {
        _lastCandidateNote = null;
        _stableCount = 0;
    }

    private static double ComputeRms(float[] samples)
    {
        var sumSquares = 0.0;
        for (var i = 0; i < samples.Length; i++)
        {
            sumSquares += samples[i] * samples[i];
        }

        return Math.Sqrt(sumSquares / samples.Length);
    }

    private static double Clamp01(double value)
        => value < 0.0 ? 0.0 : value > 1.0 ? 1.0 : value;

    private static double FrequencyToMidiNoteNumber(double frequencyHz, double a4FrequencyHz)
        => 12 * Math.Log2(frequencyHz / a4FrequencyHz) + 69;

    private static string MidiToNoteName(int midi)
    {
        var octave = (midi / 12) - 1;
        var name = NoteNames[(midi % 12 + 12) % 12];
        return $"{name}{octave}";
    }

    private static bool IsWithinPianoRange(int nearestMidi, double marginSemitones)
    {
        const double pianoMinMidi = 21.0; // A0
        const double pianoMaxMidi = 108.0; // C8

        return nearestMidi >= pianoMinMidi - marginSemitones && nearestMidi <= pianoMaxMidi + marginSemitones;
    }

    private static DetectedNoteResult CreateUnreliable(
        string noteName,
        double frequencyHz,
        double confidence,
        double centsOffset,
        double volumeRms,
        DateTime detectedAtUtc)
        => new(noteName, frequencyHz, confidence, centsOffset, volumeRms, detectedAtUtc, false);

    private static float[] PrepareSignal(float[] buffer)
    {
        var prepared = new float[buffer.Length];

        var mean = 0.0;
        for (var i = 0; i < buffer.Length; i++)
        {
            mean += buffer[i];
        }

        mean /= buffer.Length;

        var nMinus1 = Math.Max(1, buffer.Length - 1);
        for (var i = 0; i < buffer.Length; i++)
        {
            var centered = buffer[i] - mean;
            var window = 0.5 - 0.5 * Math.Cos((2.0 * Math.PI * i) / nMinus1); // Hann
            prepared[i] = (float)(centered * window);
        }

        return prepared;
    }

    private static void ApplyOnePoleLowPassInPlace(float[] samples, int sampleRate, double cutoffHz)
    {
        if (samples.Length == 0 || sampleRate <= 0 || cutoffHz <= 0)
        {
            return;
        }

        var dt = 1.0 / sampleRate;
        var rc = 1.0 / (2.0 * Math.PI * cutoffHz);
        var alpha = dt / (rc + dt);

        double y = samples[0];
        for (var i = 1; i < samples.Length; i++)
        {
            y = y + alpha * (samples[i] - y);
            samples[i] = (float)y;
        }
    }

    private static int PreferFundamentalLag(int bestLag, int minLag, int maxLag, double[] normalized, double bestScore)
    {
        var doubleLag = bestLag * 2;
        if (doubleLag <= maxLag)
        {
            var doubleScore = normalized[doubleLag - minLag];
            if (doubleScore >= bestScore * 0.72)
            {
                return doubleLag;
            }
        }

        return bestLag;
    }

    private readonly record struct DetectionSample(
        DateTime DetectedAtUtc,
        double FrequencyHz,
        double Confidence,
        string NoteName,
        double CentsOffset);
}
