namespace Machine.Plugins.Discoverers;

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

    protected override Plugin GetDiscoveredPlugin(string executablePath) => new BlenderPlugin(executablePath);
}