namespace Machine.Plugins.Discoverers;

public class PythonPluginDiscoverer : RegexPluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new List<string>
    {
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Python",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Python",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Programs\Python",
    };

    protected override string ParentDirectoryRegex => @"Python\d*";

    protected override string ExecutableRegex => @"^python(\.exe|[\d.]*)$";

    protected override Plugin GetDiscoveredPlugin(string executablePath) => new PythonPlugin(executablePath);
}
