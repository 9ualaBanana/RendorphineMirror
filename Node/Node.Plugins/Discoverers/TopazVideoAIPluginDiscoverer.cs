﻿namespace Node.Plugins.Discoverers;

internal class TopazVideoAIPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Topaz Labs LLC",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Topaz Labs LLC",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Topaz Labs LLC",
    };
    protected override string ParentDirectoryPattern => "Topaz Video AI";
    protected override string ExecutableName => "Topaz Video AI.exe";
    protected override PluginType PluginType => PluginType.TopazVideoAI;
}