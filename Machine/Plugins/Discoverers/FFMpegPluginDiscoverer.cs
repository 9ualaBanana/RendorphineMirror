namespace Machine.Plugins.Discoverers;

public class FFmpegPluginDiscoverer : IPluginDiscoverer
{
    IEnumerable<string> InstallationPathsImpl => new string[]
    {
        @"assets/",
        @"/bin/",
    };

    public IEnumerable<Plugin> Discover() =>
        InstallationPathsImpl
        .Where(Directory.Exists)
        .SelectMany(x => Directory.GetFiles(x, "ffmpeg*", SearchOption.TopDirectoryOnly))
        .Select(x => new FFmpegPlugin(x));
}