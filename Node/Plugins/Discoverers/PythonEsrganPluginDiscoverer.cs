namespace Node.Plugins.Discoverers;

public class PythonEsrganPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[] { Path.Combine("plugins", "python") };
    protected override string ParentDirectoryPattern => "esrgan";
    protected override string ExecutableName => "test.py";
    protected override PluginType PluginType => PluginType.Python_Esrgan;

    protected override string DetermineVersion(string exepath)
    {
        var verfile = Path.Combine(Path.GetDirectoryName(exepath)!, "version");
        if (!File.Exists(verfile)) return base.DetermineVersion(exepath);

        return File.ReadAllText(verfile).Trim();
    }
}
