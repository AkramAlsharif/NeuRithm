using System;

namespace Neurithm.Audio;

public sealed class SimplePitchDetector : IPitchDetector
{
    private static readonly string[] NoteNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

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
            DebounceMilliseconds = _options.DebounceMilliseconds
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
            DebounceMilliseconds = options.DebounceMilliseconds
        };

        ResetStability();
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

        if (bestLag <= 0 || bestScore <= 0)
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

        var correlationScore = normalized[correctedLag - minLag];
        var confidence = Clamp01(correlationScore);
        var noteName = ToNoteName(frequencyHz, _options.A4FrequencyHz);
        var centsOffset = CalculateCentsOffset(frequencyHz, noteName, _options.A4FrequencyHz);

        var isReliable = confidence >= _options.MinConfidence && UpdateStability(noteName, detectedAtUtc);
        return new DetectedNoteResult(noteName, frequencyHz, confidence, centsOffset, rms, detectedAtUtc, isReliable);
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

    private static string ToNoteName(double frequencyHz, double a4FrequencyHz)
    {
        var noteNumber = 12 * Math.Log2(frequencyHz / a4FrequencyHz) + 69;
        var midi = (int)Math.Round(noteNumber);
        var octave = (midi / 12) - 1;
        var name = NoteNames[(midi % 12 + 12) % 12];
        return $"{name}{octave}";
    }

    private static double CalculateCentsOffset(double frequencyHz, string detectedNoteName, double a4FrequencyHz)
    {
        var midi = NoteNameToMidi(detectedNoteName);
        var nearestFrequency = a4FrequencyHz * Math.Pow(2.0, (midi - 69) / 12.0);
        return 1200.0 * Math.Log2(frequencyHz / nearestFrequency);
    }

    private static int NoteNameToMidi(string noteName)
    {
        if (string.IsNullOrWhiteSpace(noteName) || noteName.Length < 2)
        {
            return 69;
        }

        var pitch = noteName.Length >= 3 && noteName[1] == '#'
            ? noteName[..2]
            : noteName[..1];

        var octavePart = noteName[pitch.Length..];
        if (!int.TryParse(octavePart, out var octave))
        {
            octave = 4;
        }

        var semitone = pitch switch
        {
            "C" => 0,
            "C#" => 1,
            "D" => 2,
            "D#" => 3,
            "E" => 4,
            "F" => 5,
            "F#" => 6,
            "G" => 7,
            "G#" => 8,
            "A" => 9,
            "A#" => 10,
            "B" => 11,
            _ => 9
        };

        return (octave + 1) * 12 + semitone;
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
}
