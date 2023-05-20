namespace Node.Plugins.Discoverers;

internal class VeeeVectorizerPluginDiscoverer : LocalPluginDiscoverer
{
    protected override string ExecutableName => "veee.exe";
    protected override PluginType PluginType => PluginType.VeeeVectorizer;
    protected override bool AllowExeOnLinux => true;
}
