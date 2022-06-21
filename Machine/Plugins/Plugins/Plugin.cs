namespace Machine.Plugins.Plugins;

public abstract record Plugin
{
    public abstract PluginType Type { get; }
    public string Version => _version ??= DetermineVersion();
    string _version = null!;
    public string Path { get; }

    internal Plugin(string path)
    {
        Path = path;
    }

    protected abstract string DetermineVersion();
}
