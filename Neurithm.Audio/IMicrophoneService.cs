using System;
using System.Threading.Tasks;

namespace Neurithm.Audio;

public enum MicrophonePermissionStatus
{
    NotRequested,
    Granted,
    Denied,
    Blocked,
    Error
}

public sealed record MicrophonePermissionResult(
    MicrophonePermissionStatus Status,
    string? ErrorMessage = null);

public sealed record MicrophoneInputDevice(string DeviceId, string Label, bool IsDefault);

public interface IMicrophoneService
{
    Task<MicrophonePermissionResult> RequestPermissionAsync();
    Task StartCaptureAsync(Func<float[], int, Task> onAudioFrame);
    Task StopCaptureAsync();
    Task<IReadOnlyList<MicrophoneInputDevice>> GetInputDevicesAsync();
    Task SetInputDeviceAsync(string? deviceId);
    string? SelectedInputDeviceId { get; }
    bool IsCapturing { get; }
    MicrophonePermissionStatus PermissionStatus { get; }
    string? LastError { get; }
    event Func<DetectedNoteResult, Task>? DetectionAvailable;
}
