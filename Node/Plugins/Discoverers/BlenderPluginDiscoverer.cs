namespace Node.Plugins.Discoverers;

public class BlenderPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Blender Foundation",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Blender Foundation",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Blender Foundation",
    };
    protected override string ParentDirectoryPattern => "Blender ?.?";
    protected override string ExecutableName => "blender.exe";
    protected override PluginType PluginType => PluginType.Blender;

    protected override string DetermineVersion(string exepath) => Directory.GetParent(exepath)!.Name.Split().Last();
}