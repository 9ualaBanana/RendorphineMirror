using Machine.Plugins.Plugins;

namespace Machine.Plugins.Discoverers;

public class FFMpegPluginDiscoverer : IPluginDiscoverer
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
        .Select(x => new FFMpegPlugin(x));
}