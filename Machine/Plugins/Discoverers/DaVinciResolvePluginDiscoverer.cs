using Machine.Plugins.Plugins;

namespace Machine.Plugins.Discoverers;

public class DaVinciResolvePluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        @"Program Files\Blackmagic Design",
    };
    protected override string ParentDirectoryPattern => "DaVinci Resolve";
    protected override string ExecutableName => "Resolve.exe";

    protected override Plugin DiscoveredPluginAt(string pluginExecutablePath) => new DaVinciResolvePlugin(pluginExecutablePath);
}
