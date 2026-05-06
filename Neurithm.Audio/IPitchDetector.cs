namespace Neurithm.Audio;

public sealed class PitchDetectorOptions
{
    public double A4FrequencyHz { get; set; } = 440.0;
    public double MinFrequencyHz { get; set; } = 65.0;
    public double MaxFrequencyHz { get; set; } = 2100.0;
    public double MinRmsForDetection { get; set; } = 0.006;
    public double MinConfidence { get; set; } = 0.42;
    public int MinStableDetections { get; set; } = 1;
    public int DebounceMilliseconds { get; set; } = 80;
    public double LowPassCutoffHz { get; set; } = 2400.0;
    public double PianoRangeMarginSemitones { get; set; } = 0.35;
    public double MaxCentsFromNearestNote { get; set; } = 45.0;
    public double MinCorrelationPeak { get; set; } = 0.22;
    public int SmoothingWindowMilliseconds { get; set; } = 220;
    public int FrameIntervalMilliseconds { get; set; } = 25;
    public int MinSamplesForAveraging { get; set; } = 4;
    public double DominantNoteVoteThreshold { get; set; } = 0.6;
}

public interface IPitchDetector
{
    PitchDetectorOptions GetOptions();
    void UpdateOptions(PitchDetectorOptions options);
    DetectedNoteResult AnalyzeFrame(float[] audioBuffer, int sampleRate);
}
