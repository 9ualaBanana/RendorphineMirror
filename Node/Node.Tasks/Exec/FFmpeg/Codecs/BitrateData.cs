namespace Node.Tasks.Exec.FFmpeg.Codecs;

public abstract record BitrateData
{
    public record Constant(string Bitrate) : BitrateData;
    public record Variable(string Quality) : BitrateData;
}
