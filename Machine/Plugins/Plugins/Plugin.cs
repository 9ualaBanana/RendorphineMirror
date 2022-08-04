﻿namespace Machine.Plugins.Plugins;

public abstract record Plugin
{
    public abstract PluginType Type { get; }
    public string Version => _version ??= DetermineVersion();
    string? _version;
    public string Path { get; }

    internal Plugin(string path)
    {
        Path = path;
    }

    protected virtual string DetermineVersion() => "Unknown";
}
