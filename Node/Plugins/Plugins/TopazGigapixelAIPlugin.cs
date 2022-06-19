namespace Node.Plugins.Plugins;

internal record TopazGigapixelAIPlugin : Plugin
{
    public TopazGigapixelAIPlugin(string path) : base(path)
    {
    }

    internal override PluginType Type => PluginType.TopazGigapixelAI;

    protected override string DetermineVersion() => "Unknown";
}
