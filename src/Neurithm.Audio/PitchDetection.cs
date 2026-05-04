namespace Neurithm.Audio.Interop;

/// <summary>
/// Abstraction for browser audio/microphone access.
/// JavaScript interop is handled here, keeping business logic in C#.
/// </summary>
public interface IAudioInterop
{
    /// <summary>
    /// Request microphone permission from the browser.
    /// Returns true if permission was granted.
    /// </summary>
    Task<bool> RequestMicrophonePermissionAsync();

    /// <summary>
    /// Check if microphone is available and permitted.
    /// </summary>
    Task<bool> IsMicrophoneAvailableAsync();

    /// <summary>
    /// Start capturing audio from microphone.
    /// </summary>
    Task StartAudioCaptureAsync();

    /// <summary>
    /// Stop capturing audio from microphone.
    /// </summary>
    Task StopAudioCaptureAsync();

    /// <summary>
    /// Get the latest audio buffer.
    /// Returns float array of PCM samples.
    /// </summary>
    Task<float[]?> GetAudioBufferAsync();

    /// <summary>
    /// Get the current input volume level (RMS).
    /// </summary>
    Task<double> GetInputVolumeAsync();
}

/// <summary>
/// Event args for detected pitch.
/// </summary>
public record PitchDetectedEventArgs(double FrequencyHz, double Confidence, double Volume, long TimestampMs);

/// <summary>
/// Core pitch detection engine.
/// Processes PCM audio data and extracts pitch information.
/// </summary>
public interface IPitchDetector
{
    /// <summary>
    /// Process audio buffer and detect pitch.
    /// </summary>
    PitchDetectedEventArgs? DetectPitch(float[] audioBuffer, int sampleRateHz);

    /// <summary>
    /// Calculate RMS (volume) of audio buffer.
    /// </summary>
    double CalculateRMS(float[] audioBuffer);

    /// <summary>
    /// Calculate autocorrelation for pitch detection.
    /// </summary>
    double[] CalculateAutocorrelation(float[] audioBuffer);
}

/// <summary>
/// Implementation of pitch detection using autocorrelation method.
/// </summary>
public class AutocorrelationPitchDetector : IPitchDetector
{
    private const double MinConfidence = 0.1;

    public PitchDetectedEventArgs? DetectPitch(float[] audioBuffer, int sampleRateHz)
    {
        if (audioBuffer.Length < 2)
            return null;

        var rms = CalculateRMS(audioBuffer);
        if (rms < 0.001) // Silence threshold
            return null;

        // Calculate autocorrelation
        var acf = CalculateAutocorrelation(audioBuffer);

        // Find the first peak after lag 0 (which is 1.0)
        var minLag = sampleRateHz / 4000; // Max 4000 Hz for MVP
        var maxLag = sampleRateHz / 32;   // Min 32 Hz

        if (minLag >= acf.Length || maxLag >= acf.Length)
            return null;

        var bestLag = minLag;
        var bestValue = acf[minLag];

        for (int i = minLag; i < maxLag && i < acf.Length; i++)
        {
            if (acf[i] > bestValue)
            {
                bestValue = acf[i];
                bestLag = i;
            }
        }

        var confidence = Math.Max(0, bestValue);
        if (confidence < MinConfidence)
            return null;

        // Convert lag to frequency
        var frequency = sampleRateHz / (double)bestLag;

        return new PitchDetectedEventArgs(
            FrequencyHz: frequency,
            Confidence: Math.Min(1.0, confidence),
            Volume: rms,
            TimestampMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        );
    }

    public double CalculateRMS(float[] audioBuffer)
    {
        if (audioBuffer.Length == 0)
            return 0;

        double sumSquares = 0;
        foreach (var sample in audioBuffer)
        {
            sumSquares += sample * sample;
        }

        return Math.Sqrt(sumSquares / audioBuffer.Length);
    }

    public double[] CalculateAutocorrelation(float[] audioBuffer)
    {
        var size = audioBuffer.Length / 2;
        var result = new double[size];

        for (int lag = 0; lag < size; lag++)
        {
            double sum = 0;
            for (int i = 0; i < size; i++)
            {
                sum += audioBuffer[i] * audioBuffer[i + lag];
            }
            result[lag] = sum / size;
        }

        // Normalize by first value
        if (result[0] > 0)
        {
            for (int i = 0; i < result.Length; i++)
            {
                result[i] /= result[0];
            }
        }

        return result;
    }
}
