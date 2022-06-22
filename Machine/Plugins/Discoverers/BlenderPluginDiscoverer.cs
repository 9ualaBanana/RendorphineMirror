﻿using Machine.Plugins.Plugins;

namespace Machine.Plugins.Discoverers;

public class BlenderPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        @"Program Files\Blender Foundation",
    };
    protected override string ParentDirectoryPattern => "Blender ?.?";
    protected override string ExecutableName => "blender.exe";

    protected override Plugin DiscoveredPluginAt(string pluginExecutablePath) => new BlenderPlugin(pluginExecutablePath);
}