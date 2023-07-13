namespace Node.Tasks.Exec.FFmpeg;

public class FFmpegInput
{
    public string Path { get; }
    public FFProbe.FFProbeInfo FFProbe { get; }

    public FFmpegInput(string path, FFProbe.FFProbeInfo fFProbe)
    {
        Path = path;
        FFProbe = fFProbe;
    }

    public static async Task<FFmpegInput> FromAsync(string path, ILoggable? logger) =>
        new(path, await Node.Common.FFProbe.Get(path, logger));
}
