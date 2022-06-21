namespace Machine.Plugins.Plugins;

internal record TopazGigapixelAIPlugin : Plugin
{
    public TopazGigapixelAIPlugin(string path) : base(path)
    {
    }

    public override PluginType Type => PluginType.TopazGigapixelAI;

    protected override string DetermineVersion() => "Unknown";
}
