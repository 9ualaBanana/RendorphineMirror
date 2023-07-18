namespace Node.Tasks.Exec.FFmpeg.Codecs;

public interface IFFmpegCodec
{
    IFFmpegCodec? Fallback => null;

    IEnumerable<string> BuildInputArgs() => Enumerable.Empty<string>();
    IEnumerable<string> BuildOutputArgs();
}
