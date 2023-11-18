namespace Node.Tasks.Exec.FFmpeg.Codecs;

public class NullFFmpegCodec : IFFmpegCodec
{
    public IEnumerable<string> BuildOutputArgs()
    {
        return new ArgList() { "-f", "null", };
    }
}
