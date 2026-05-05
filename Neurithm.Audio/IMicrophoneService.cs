using System;
using System.Threading.Tasks;

namespace Neurithm.Audio
{
    public interface IMicrophoneService
    {
        Task<bool> RequestPermissionAsync();
        Task StartCaptureAsync(Func<float[], int, Task> onAudioFrame);
        Task StopCaptureAsync();
        bool IsCapturing { get; }
    }
}
