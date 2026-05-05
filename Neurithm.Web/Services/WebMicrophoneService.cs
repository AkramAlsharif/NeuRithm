using Microsoft.JSInterop;
using Neurithm.Audio;
using Neurithm.Web.Models;

namespace Neurithm.Web.Services;

public sealed class WebMicrophoneService : IMicrophoneService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<WebMicrophoneService>? _dotNetRef;
    private Func<float[], int, Task>? _onAudioFrame;

    public WebMicrophoneService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsCapturing { get; private set; }

    public event Func<string, Task>? NoteDetected;

    public async Task<bool> RequestPermissionAsync()
    {
        return await _jsRuntime.InvokeAsync<bool>("neurithmMicrophone.requestPermission");
    }

    public async Task StartCaptureAsync(Func<float[], int, Task> onAudioFrame)
    {
        if (IsCapturing)
        {
            return;
        }

        _onAudioFrame = onAudioFrame;
        _dotNetRef ??= DotNetObjectReference.Create(this);

        var started = await _jsRuntime.InvokeAsync<bool>("neurithmMicrophone.startCapture", _dotNetRef);
        IsCapturing = started;
    }

    public async Task StopCaptureAsync()
    {
        if (!IsCapturing)
        {
            return;
        }

        await _jsRuntime.InvokeVoidAsync("neurithmMicrophone.stopCapture");
        IsCapturing = false;
    }

    [JSInvokable]
    public async Task OnAudioFrame(MicrophoneFrameDto frame)
    {
        if (_onAudioFrame is null)
        {
            return;
        }

        await _onAudioFrame(frame.Samples, frame.SampleRate);

        var note = DetectDominantNote(frame.Samples, frame.SampleRate);
        if (!string.IsNullOrWhiteSpace(note) && NoteDetected is not null)
        {
            await NoteDetected.Invoke(note);
        }
    }

    private static string? DetectDominantNote(float[] samples, int sampleRate)
    {
        if (samples.Length == 0 || sampleRate <= 0)
        {
            return null;
        }

        var crossings = 0;
        for (var i = 1; i < samples.Length; i++)
        {
            if ((samples[i - 1] <= 0 && samples[i] > 0) || (samples[i - 1] >= 0 && samples[i] < 0))
            {
                crossings++;
            }
        }

        if (crossings < 2)
        {
            return null;
        }

        var estimatedFrequency = (crossings * sampleRate) / (2.0 * samples.Length);
        if (estimatedFrequency < 65.0 || estimatedFrequency > 2100.0)
        {
            return null;
        }

        return FrequencyToNoteName(estimatedFrequency);
    }

    private static string FrequencyToNoteName(double frequency)
    {
        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var noteNumber = 12 * Math.Log2(frequency / 440.0) + 69;
        var rounded = (int)Math.Round(noteNumber);
        var octave = (rounded / 12) - 1;
        var name = noteNames[(rounded % 12 + 12) % 12];
        return $"{name}{octave}";
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (IsCapturing)
            {
                await StopCaptureAsync();
            }
        }
        catch
        {
            // ignore disposal errors
        }

        _dotNetRef?.Dispose();
    }
}
