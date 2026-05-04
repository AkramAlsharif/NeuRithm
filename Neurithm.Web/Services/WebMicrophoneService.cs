using Microsoft.JSInterop;
using Neurithm.Audio;

namespace Neurithm.Web.Services;

public sealed class WebMicrophoneService : IMicrophoneService
{
    private readonly IJSRuntime _jsRuntime;

    public WebMicrophoneService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsCapturing { get; private set; }

    public async Task<bool> RequestPermissionAsync()
    {
        return await _jsRuntime.InvokeAsync<bool>("neurithmMicrophone.requestPermission");
    }

    public Task StartCaptureAsync(Func<float[], Task> onAudioFrame)
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
