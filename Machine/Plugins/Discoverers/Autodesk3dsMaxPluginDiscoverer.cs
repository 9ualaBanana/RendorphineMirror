namespace Machine.Plugins.Discoverers;

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

    protected override Plugin GetDiscoveredPlugin(string executablePath) => new Autodesk3dsMaxPlugin(executablePath);
}