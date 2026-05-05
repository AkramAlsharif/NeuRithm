namespace Neurithm.Web.Models;

public sealed class MicrophoneFrameDto
{
    public float[] Samples { get; set; } = [];
    public int SampleRate { get; set; }
}
