namespace Node.Plugins.Plugins;

internal abstract record Plugin
{
    internal abstract PluginType Type { get; }
    internal string Version => _version ??= DetermineVersion();
    string _version = null!;
    readonly internal string Path;

    internal Plugin(string path)
    {
        Path = path;
    }

    protected abstract string DetermineVersion();
}
