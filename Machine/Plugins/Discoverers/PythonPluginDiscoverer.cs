namespace Machine.Plugins.Discoverers;

public class PythonPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new List<string>
    {
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}\Python",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}\Python",
        $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Programs\Python",
    };
    protected override string ParentDirectoryRegex => @"Python\d*";
    protected override string ExecutableRegex => @"^python(\.exe|[\d.]*)$";
    protected override PluginType PluginType => PluginType.Python;

    protected override string DetermineVersion(string exepath)
    {
        var data = StartProcess(exepath, "--version").AsSpan();

        // Python 3.10.6
        data = data.Slice("Python ".Length);

        return new string(data.Trim());
    }
}
