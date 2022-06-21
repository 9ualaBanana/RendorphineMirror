namespace Machine.Plugins.Plugins;

internal record DaVinciResolvePlugin : Plugin
{
    public DaVinciResolvePlugin(string path) : base(path)
    {
    }

    public override PluginType Type => PluginType.DaVinciResolve;

    protected override string DetermineVersion() => "Unknown";
}
