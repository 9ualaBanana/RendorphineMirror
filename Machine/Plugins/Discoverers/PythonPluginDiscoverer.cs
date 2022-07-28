namespace Machine.Plugins.Discoverers;

public class PythonPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new List<string>
    {
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Python",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Python",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Programs\Python",
    };

    protected override string ParentDirectoryPattern => "Python*";

    protected override string ExecutableName => "python.exe";

    protected override Plugin GetDiscoveredPlugin(string executablePath) => new PythonPlugin(executablePath);
}
