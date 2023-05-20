namespace Node.Plugins.Discoverers;

internal class RobustVideoMattingPluginDiscoverer : LocalPluginDiscoverer
{
    protected override string ExecutableName => "inference.py";
    protected override PluginType PluginType => PluginType.RobustVideoMatting;
}
