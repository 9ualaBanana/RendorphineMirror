namespace Node.Plugins.Discoverers;

public class CondaPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => Enumerable.Empty<string>();
    protected override string ParentDirectoryPattern => "mamba";
    protected override string ExecutableRegex => @"micromamba(\.exe)?";
    protected override PluginType PluginType => PluginType.Conda;

    protected override string DetermineVersion(string exepath)
    {
        // 1.4.0
        return Processes.FullExecute(exepath, "--version", Logger.AsLoggable()).Result.Trim();
    }
}