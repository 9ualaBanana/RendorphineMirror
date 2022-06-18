using Node.Plugins.Plugins;

namespace Node.Plugins.Discoverers;

internal class TopazGigapixelAIPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        @"C:\Program Files\Topaz Labs LLC",
    };
    protected override string ParentDirectoryPattern => "Topaz Video Enhance AI";
    protected override string ExecutableName => "Topaz Video Enhance AI.exe";

    protected override Plugin DiscoveredPluginAt(string pluginExecutablePath) => new TopazGigapixelAIPlugin(pluginExecutablePath);
}
