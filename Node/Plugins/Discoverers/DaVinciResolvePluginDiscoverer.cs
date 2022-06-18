using Node.Plugins.Plugins;

namespace Node.Plugins.Discoverers;

internal class DaVinciResolvePluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        @"C:\Program Files\Blackmagic Design",
    };
    protected override string ParentDirectoryPattern => "DaVinci Resolve";
    protected override string ExecutableName => "Resolve.exe";

    protected override Plugin DiscoveredPluginAt(string pluginExecutablePath) => new DaVinciResolvePlugin(pluginExecutablePath);
}
