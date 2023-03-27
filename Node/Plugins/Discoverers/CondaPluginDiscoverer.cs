namespace Node.Plugins.Discoverers;

public class CondaPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "miniconda3"),
        "/opt/miniconda3/bin",
    };
    protected override string ParentDirectoryPattern => "Scripts";
    protected override string ExecutableRegex => @"conda(\.exe)?";
    protected override PluginType PluginType => PluginType.Conda;

    protected override string DetermineVersion(string exepath)
    {
        // conda 23.1.0
        var exec = Processes.FullExecute(exepath, "--version", Logger.AsLoggable()).Result;
        return exec.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1];
    }
}