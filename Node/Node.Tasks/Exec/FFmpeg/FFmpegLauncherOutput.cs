namespace Node.Tasks.Exec.FFmpeg;

public record FFmpegLauncherOutput
{
    public ArgList Args { get; init; } = new();
    public required IFFmpegCodec? Codec { get; init; }
    public required string? Output { get; init; }


    public ArgList Build(bool fallback) =>
        new ArgList()
        {
            Args,
            Codec is null ? null : (fallback ? (Codec.Fallback ?? Codec) : Codec).BuildOutputArgs(),
            Output,
        };
}
