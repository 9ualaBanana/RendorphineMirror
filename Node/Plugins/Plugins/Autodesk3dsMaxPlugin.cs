using Node.Plugins.Discoverers;

namespace Node.Plugins.Plugins;

internal record Autodesk3dsMaxPlugin : Plugin
{
    public Autodesk3dsMaxPlugin(string path) : base(path)
    {
    }

    protected override PluginDiscoverer DiscovererImpl => new Autodesk3dsMaxPluginDiscoverer();

    internal override PluginType Type => PluginType.Autodesk3dsMax;

    protected override string DetermineVersion() => Directory.GetParent(Path)!.Name.Split().Last();
}
