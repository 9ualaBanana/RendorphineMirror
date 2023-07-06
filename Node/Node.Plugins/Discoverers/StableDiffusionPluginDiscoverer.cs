namespace Node.Plugins.Discoverers;

internal class StableDiffusionPluginDiscoverer : LocalPluginDiscoverer
{
    protected override string ExecutableRegex => @"sdcli(\.exe)?";
    protected override PluginType PluginType => PluginType.StableDiffusion;
}
