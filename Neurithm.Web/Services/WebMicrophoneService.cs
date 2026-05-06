using Microsoft.JSInterop;
using Neurithm.Audio;
using Neurithm.Web.Models;

namespace Neurithm.Web.Services;

public sealed class WebMicrophoneService : IMicrophoneService, IAsyncDisposable
{
    private const string SelectedMicStorageKey = "neurithm_selected_microphone";

    private readonly IJSRuntime _jsRuntime;
    private readonly IPitchDetector _pitchDetector;

    private DotNetObjectReference<WebMicrophoneService>? _dotNetRef;
    private Func<float[], int, Task>? _onAudioFrame;

    public WebMicrophoneService(IJSRuntime jsRuntime, IPitchDetector pitchDetector)
    {
        _jsRuntime = jsRuntime;
        _pitchDetector = pitchDetector;
    }

    public bool IsCapturing { get; private set; }

    public MicrophonePermissionStatus PermissionStatus { get; private set; } = MicrophonePermissionStatus.NotRequested;

    public string? LastError { get; private set; }

    public string? SelectedInputDeviceId { get; private set; }

    public event Func<DetectedNoteResult, Task>? DetectionAvailable;

    public async Task<MicrophonePermissionResult> RequestPermissionAsync()
    {
        var response = await _jsRuntime.InvokeAsync<MicrophonePermissionJsResponse>("neurithmMicrophone.requestPermission");
        PermissionStatus = MapPermissionStatus(response.Status);
        LastError = response.ErrorMessage;
        return new MicrophonePermissionResult(PermissionStatus, LastError);
    }

    public async Task<IReadOnlyList<MicrophoneInputDevice>> GetInputDevicesAsync()
    {
        try
        {
            SelectedInputDeviceId ??= await _jsRuntime.InvokeAsync<string?>("neurithmMicrophone.getSavedInputDevice", SelectedMicStorageKey);
            var devices = await _jsRuntime.InvokeAsync<MicrophoneInputDeviceJsResponse[]>("neurithmMicrophone.getInputDevices");

            return devices.Select(d => new MicrophoneInputDevice(
                d.DeviceId,
                string.IsNullOrWhiteSpace(d.Label) ? "Microphone" : d.Label,
                d.IsDefault)).ToArray();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return Array.Empty<MicrophoneInputDevice>();
        }
    }

    public async Task SetInputDeviceAsync(string? deviceId)
    {
        SelectedInputDeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId;
        await _jsRuntime.InvokeVoidAsync("neurithmMicrophone.saveInputDevice", SelectedMicStorageKey, SelectedInputDeviceId);
    }

    public async Task StartCaptureAsync(Func<float[], int, Task> onAudioFrame)
    {
        if (IsCapturing)
        {
            return;
        }

        _onAudioFrame = onAudioFrame;
        _dotNetRef ??= DotNetObjectReference.Create(this);
        SelectedInputDeviceId ??= await _jsRuntime.InvokeAsync<string?>("neurithmMicrophone.getSavedInputDevice", SelectedMicStorageKey);

        var startResponse = await _jsRuntime.InvokeAsync<CaptureStartJsResponse>("neurithmMicrophone.startCapture", _dotNetRef, SelectedInputDeviceId);
        IsCapturing = startResponse.Started;

        if (IsCapturing)
        {
            PermissionStatus = MicrophonePermissionStatus.Granted;
            LastError = null;
            return;
        }

        PermissionStatus = startResponse.Blocked ? MicrophonePermissionStatus.Blocked : MicrophonePermissionStatus.Error;
        LastError = string.IsNullOrWhiteSpace(startResponse.ErrorMessage)
            ? "Failed to start microphone capture."
            : startResponse.ErrorMessage;
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
        if (frame.Samples.Length == 0 || frame.SampleRate <= 0)
        {
            return;
        }

        if (_onAudioFrame is not null)
        {
            await _onAudioFrame(frame.Samples, frame.SampleRate);
        }

        var detection = _pitchDetector.AnalyzeFrame(frame.Samples, frame.SampleRate);
        if (DetectionAvailable is not null)
        {
            await DetectionAvailable.Invoke(detection);
        }
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

    private static MicrophonePermissionStatus MapPermissionStatus(string? status)
        => status?.ToLowerInvariant() switch
        {
            "granted" => MicrophonePermissionStatus.Granted,
            "denied" => MicrophonePermissionStatus.Denied,
            "blocked" => MicrophonePermissionStatus.Blocked,
            "error" => MicrophonePermissionStatus.Error,
            _ => MicrophonePermissionStatus.NotRequested
        };

    private sealed class MicrophonePermissionJsResponse
    {
        public string Status { get; set; } = "notrequested";
        public string? ErrorMessage { get; set; }
    }

    private sealed class CaptureStartJsResponse
    {
        public bool Started { get; set; }
        public bool Blocked { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private sealed class MicrophoneInputDeviceJsResponse
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
