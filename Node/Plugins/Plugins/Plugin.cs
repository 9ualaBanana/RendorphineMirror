using Node.Plugins.Discoverers;

namespace Node.Plugins.Plugins;

internal abstract record Plugin
{
    internal abstract PluginType Type { get; }
    internal string Version => _version ??= DetermineVersion();
    string _version = null!;
    readonly internal string Path;
    internal PluginDiscoverer Discoverer => _discoverer ??= DiscovererImpl;
    PluginDiscoverer? _discoverer;
    protected abstract PluginDiscoverer DiscovererImpl { get; }

    internal Plugin(string path)
    {
        Path = path;
    }

    protected abstract string DetermineVersion();
}
