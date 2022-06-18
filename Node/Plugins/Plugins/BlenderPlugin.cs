using Node.Plugins.Discoverers;

namespace Node.Plugins.Plugins;

internal record BlenderPlugin : Plugin
{
    internal override PluginType Type => PluginType.Blender;
    protected override PluginDiscoverer DiscovererImpl => new BlenderPluginDiscoverer();

    public BlenderPlugin(string path) : base(path)
    {
    }

    protected override string DetermineVersion() => Directory.GetParent(Path)!.Name.Split().Last();
}
