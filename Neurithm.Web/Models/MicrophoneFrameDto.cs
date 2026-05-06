using System.Text.Json.Serialization;

namespace Neurithm.Web.Models;

public sealed class MicrophoneFrameDto
{
    [JsonPropertyName("samples")]
    public float[] Samples { get; set; } = [];

    [JsonPropertyName("sampleRate")]
    public int SampleRate { get; set; }
}
