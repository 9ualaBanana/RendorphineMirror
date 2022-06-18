using Node.Plugins.Plugins;

namespace Node.Plugins.Discoverers;

internal class Autodesk3dsMaxPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        @"C:\Program Files\Autodesk",
    };

    protected override string ParentDirectoryPattern => "3ds Max 2018 ????";

    protected override string ExecutableName => "3dsmax.exe";

    protected override Plugin DiscoveredPluginAt(string pluginExecutablePath) => new Autodesk3dsMaxPlugin(pluginExecutablePath);
}