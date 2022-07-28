namespace Machine.Plugins.Discoverers;

public abstract class PluginDiscoverer : IPluginDiscoverer
{
    protected IEnumerable<string> InstallationPaths => _installationPaths ??=
        InstallationPathsImpl.Select(Path.TrimEndingDirectorySeparator);
    IEnumerable<string>? _installationPaths;
    protected abstract IEnumerable<string> InstallationPathsImpl { get; }
    protected abstract string ParentDirectoryPattern { get; }
    protected abstract string ExecutableName { get; }

    public IEnumerable<Plugin> Discover()
    {
        return InstallationPaths
            .Where(Directory.Exists)
            .SelectMany(installationPath => Directory.EnumerateDirectories(
                installationPath,
                ParentDirectoryPattern,
                SearchOption.TopDirectoryOnly))
            .SelectMany(pluginDirectory => Directory.EnumerateFiles(
                pluginDirectory,
                ExecutableName,
                SearchOption.TopDirectoryOnly))
            .Select(GetDiscoveredPlugin);
    }

    protected abstract Plugin GetDiscoveredPlugin(string executablePath);
}
