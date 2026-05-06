using System;
using System.Threading.Tasks;

namespace Neurithm.Audio;

public sealed class BrowserMicrophoneService : IMicrophoneService
{
    public bool IsCapturing { get; private set; }

    public MicrophonePermissionStatus PermissionStatus { get; private set; } = MicrophonePermissionStatus.NotRequested;

    public string? LastError { get; private set; }

    public string? SelectedInputDeviceId { get; private set; }

    public event Func<DetectedNoteResult, Task>? DetectionAvailable;

    public Task<MicrophonePermissionResult> RequestPermissionAsync()
    {
        PermissionStatus = MicrophonePermissionStatus.Blocked;
        LastError = "Browser microphone capture must be provided by Web interop implementation.";
        return Task.FromResult(new MicrophonePermissionResult(PermissionStatus, LastError));
    }

    public Task<IReadOnlyList<MicrophoneInputDevice>> GetInputDevicesAsync()
        => Task.FromResult<IReadOnlyList<MicrophoneInputDevice>>(Array.Empty<MicrophoneInputDevice>());

    public Task SetInputDeviceAsync(string? deviceId)
    {
        SelectedInputDeviceId = deviceId;
        return Task.CompletedTask;
    }

    public Task StartCaptureAsync(Func<float[], int, Task> onAudioFrame)
    {
        IsCapturing = true;
        return Task.CompletedTask;
    }

    public Task StopCaptureAsync()
    {
        IsCapturing = false;
        return Task.CompletedTask;
    }
}
