namespace Node.Plugins;

public class PluginDirs
{
    public string Directory { get; }

    public PluginDirs(string directory) => Directory = Directories.DirCreated(directory);
}
