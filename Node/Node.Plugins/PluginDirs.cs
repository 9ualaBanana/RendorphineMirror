namespace Node.Plugins;

public class PluginDirs
{
    public string Directory { get; }

    public PluginDirs(string directory) => Directory = Directories.DirCreated(directory);

    public string GetPluginDirectory(PluginType type, PluginVersion version, bool isLatest)
    {
        var versionstr = isLatest ? "latest" : version.ToString().ToLowerInvariant();
        return Directories.DirCreated(Directory, type.ToString().ToLowerInvariant(), versionstr);
    }
}
