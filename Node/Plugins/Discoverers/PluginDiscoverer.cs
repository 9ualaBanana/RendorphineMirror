using Node.Plugins.Plugins;

namespace Node.Plugins.Discoverers;

internal abstract class PluginDiscoverer
{
    protected IEnumerable<string> InstallationPaths => _installationPaths ??=
        InstallationPathsImpl.Select(path => Path.TrimEndingDirectorySeparator(path));
    IEnumerable<string>? _installationPaths;
    protected abstract IEnumerable<string> InstallationPathsImpl { get; }
    protected abstract string ParentDirectoryPattern { get; }
    protected abstract string ExecutableName { get; }

    internal IEnumerable<Plugin> Discover()
    {
        return InstallationPaths
            .Where(installationPath => Directory.Exists(installationPath))
            .SelectMany(installationPath => Directory.EnumerateDirectories(
                installationPath,
                ParentDirectoryPattern,
                SearchOption.TopDirectoryOnly))
            .SelectMany(pluginDirectory => Directory.EnumerateFiles(
                pluginDirectory,
                ExecutableName,
                SearchOption.TopDirectoryOnly))
            .Select(pluginExecutablePath => DiscoveredPluginAt(pluginExecutablePath));
    }

    protected abstract Plugin DiscoveredPluginAt(string pluginExecutablePath);
}
