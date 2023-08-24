namespace Node.Plugins.Discoverers;

internal class Yolov7PluginDiscoverer : LocalPluginDiscoverer
{
    protected override string ExecutableRegex => @"yolov7cli(\.exe)?";
    protected override PluginType PluginType => PluginType.Yolov7;
}
