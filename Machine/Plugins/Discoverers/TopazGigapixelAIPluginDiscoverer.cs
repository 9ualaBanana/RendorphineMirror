namespace Machine.Plugins.Discoverers;

public class TopazGigapixelAIPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Topaz Labs LLC",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Topaz Labs LLC",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Topaz Labs LLC",
    };
    protected override string ParentDirectoryPattern => "Topaz Video Enhance AI";
    protected override string ExecutableName => "Topaz Video Enhance AI.exe";

    protected override Plugin GetDiscoveredPlugin(string executablePath) => new TopazGigapixelAIPlugin(executablePath);
}
