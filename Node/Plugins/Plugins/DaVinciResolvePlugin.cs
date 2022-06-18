using Node.Plugins.Discoverers;

namespace Node.Plugins.Plugins;

internal record DaVinciResolvePlugin : Plugin
{
    public DaVinciResolvePlugin(string path) : base(path)
    {
    }

    protected override PluginDiscoverer DiscovererImpl => new DaVinciResolvePluginDiscoverer();
    internal override PluginType Type => PluginType.DaVinciResolve;

    protected override string DetermineVersion() => "Unknown";
}
