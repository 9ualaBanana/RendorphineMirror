namespace Node.Plugins.Models;

/// <summary> Struct to compare plugin versions </summary>
public readonly struct PluginVersion : IComparable<PluginVersion>
{
    readonly string VersionString;
    readonly Version? Version;

    public PluginVersion(string versionString, Version? version)
    {
        VersionString = versionString;
        Version = version;
    }

    public static PluginVersion From(Plugin plugin) => Parse(plugin.Version);
    public static PluginVersion Parse(string version)
    {
        Version.TryParse(version, out var ver);
        return new(version, ver);
    }


    public int CompareTo(PluginVersion other)
    {
        if (Version is not null && other.Version is not null)
            return Version.CompareTo(other.Version);

        return VersionString.CompareTo(other.VersionString);
    }
}
