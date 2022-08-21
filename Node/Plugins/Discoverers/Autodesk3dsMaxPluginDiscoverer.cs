namespace Node.Plugins.Discoverers;

public class Autodesk3dsMaxPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new List<string>
    {
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Autodesk",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Autodesk",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Autodesk",
    };
    protected override string ParentDirectoryPattern => "3ds Max ????";
    protected override string ExecutableName => "3dsmax.exe";
    protected override PluginType PluginType => PluginType.Autodesk3dsMax;

    protected override string DetermineVersion(string exepath) => Directory.GetParent(exepath)!.Name.Split().Last();
}