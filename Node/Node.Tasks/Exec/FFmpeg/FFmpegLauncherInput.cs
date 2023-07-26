namespace Node.Tasks.Exec.FFmpeg;

public class FFmpegLauncherInput
{
    public string Path { get; }
    public ArgList Args { get; } = new();

    public FFmpegLauncherInput(string path) => Path = path;
}
