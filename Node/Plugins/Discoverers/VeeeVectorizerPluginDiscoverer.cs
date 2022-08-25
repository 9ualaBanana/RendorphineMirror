namespace Node.Plugins.Discoverers;

public class VeeeVectorizerPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[] { Path.Combine("plugins") };
    protected override string ParentDirectoryPattern => "veeevectorizer";
    protected override string ExecutableName => "veee.exe";
    protected override PluginType PluginType => PluginType.VeeeVectorizer;

    protected override string DetermineVersion(string exepath)
    {
        var verfile = Path.Combine(Path.GetDirectoryName(exepath)!, "version");
        if (!File.Exists(verfile)) return base.DetermineVersion(exepath);

        return File.ReadAllText(verfile).Trim();
    }
}
