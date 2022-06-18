using Node.Plugins.Discoverers;

namespace Node.Plugins.Plugins;

internal record TopazGigapixelAIPlugin : Plugin
{
    public TopazGigapixelAIPlugin(string path) : base(path)
    {
    }

    protected override PluginDiscoverer DiscovererImpl => new TopazGigapixelAIPluginDiscoverer();

    internal override PluginType Type => PluginType.TopazGigapixelAI;

    protected override string DetermineVersion() => "Unknown";
}
