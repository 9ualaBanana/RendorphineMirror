namespace Node.Plugins.Discoverers;

public class PythonEsrganPluginDiscoverer : LocalPluginDiscoverer
{
    protected override string ExecutableName => "test.py";
    protected override PluginType PluginType => PluginType.Esrgan;
}
