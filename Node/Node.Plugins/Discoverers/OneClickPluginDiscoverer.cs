
namespace Node.Plugins.Discoverers;

public class OneClickPluginDiscoverer : LocalPluginDiscoverer
{
    protected override string? ExecutableRegex => @"oneclickexport.*\.mzp";
    protected override PluginType PluginType => PluginType.OneClick;
}
