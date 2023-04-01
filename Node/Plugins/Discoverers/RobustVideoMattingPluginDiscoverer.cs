namespace Node.Plugins.Discoverers;

public class RobustVideoMattingPluginDiscoverer : LocalPluginDiscoverer
{
    protected override string ExecutableName => "inference.py";
    protected override PluginType PluginType => PluginType.RobustVideoMatting;
}
