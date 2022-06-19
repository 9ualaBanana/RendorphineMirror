namespace Node.Plugins.Plugins;

internal record BlenderPlugin : Plugin
{
    public BlenderPlugin(string path) : base(path)
    {
    }

    internal override PluginType Type => PluginType.Blender;

    protected override string DetermineVersion() => Directory.GetParent(Path)!.Name.Split().Last();
}
