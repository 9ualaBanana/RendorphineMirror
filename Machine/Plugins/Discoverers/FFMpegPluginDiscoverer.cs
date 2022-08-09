namespace Machine.Plugins.Discoverers;

public class FFmpegPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        @"assets/",
    };
    protected override string ExecutableName => "ffmpeg*";

    protected override Plugin GetDiscoveredPlugin(string executablePath) => new FFmpegPlugin(executablePath);
}