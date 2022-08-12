namespace Machine.Plugins.Discoverers;

public class DaVinciResolvePluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Blackmagic Design",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Blackmagic Design",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Blackmagic Design",
    };
    protected override string ParentDirectoryPattern => "DaVinci Resolve";
    protected override string ExecutableName => "Resolve.exe";
    protected override PluginType PluginType => PluginType.DaVinciResolve;
}
