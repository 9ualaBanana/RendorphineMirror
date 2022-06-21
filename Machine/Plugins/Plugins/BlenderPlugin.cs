namespace Machine.Plugins.Plugins;

internal record BlenderPlugin : Plugin
{
    public BlenderPlugin(string path) : base(path)
    {
    }

    public override PluginType Type => PluginType.Blender;

    protected override string DetermineVersion() => Directory.GetParent(Path)!.Name.Split().Last();
}
