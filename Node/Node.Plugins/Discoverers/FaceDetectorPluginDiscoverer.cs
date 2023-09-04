namespace Node.Plugins.Discoverers;

internal class ImageDetectorPluginDiscoverer : LocalPluginDiscoverer
{
    protected override string ExecutableName => "main.py";
    protected override PluginType PluginType => PluginType.ImageDetector;
}
