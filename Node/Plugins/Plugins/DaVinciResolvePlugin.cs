namespace Node.Plugins.Plugins;

internal record DaVinciResolvePlugin : Plugin
{
    public DaVinciResolvePlugin(string path) : base(path)
    {
    }

    internal override PluginType Type => PluginType.DaVinciResolve;

    protected override string DetermineVersion() => "Unknown";
}
