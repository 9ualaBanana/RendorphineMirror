using Machine.Plugins.Plugins;

namespace Machine.Plugins.Discoverers;

public class Autodesk3dsMaxPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        @"Program Files\Autodesk",
    };

    protected override string ParentDirectoryPattern => "3ds Max ????";

    protected override string ExecutableName => "3dsmax.exe";

    protected override Plugin DiscoveredPluginAt(string pluginExecutablePath) => new Autodesk3dsMaxPlugin(pluginExecutablePath);
}