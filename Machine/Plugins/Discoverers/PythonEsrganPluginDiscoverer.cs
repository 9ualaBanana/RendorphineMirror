namespace Machine.Plugins.Discoverers;

public class PythonEsrganPluginDiscoverer : PluginDiscoverer
{
    protected override IEnumerable<string> InstallationPathsImpl => new string[] { Path.Combine("plugins", "python") };

    protected override string ParentDirectoryPattern => "ESRGAN";

    protected override string ExecutableName => "test.py";

    protected override Plugin GetDiscoveredPlugin(string executablePath) => new PythonEsrganPlugin(executablePath);
}
