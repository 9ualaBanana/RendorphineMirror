using Common.Plugins;

namespace Node.Plugins.Discoverers;

public class VeeeVectorizerPluginDiscoverer : LocalPluginDiscoverer
{
    protected override string ParentDirectoryPattern => "veeevectorizer";
    protected override string ExecutableName => "veee.exe";
    protected override PluginType PluginType => PluginType.VeeeVectorizer;
}
