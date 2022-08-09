using System.Text.RegularExpressions;

namespace Machine.Plugins.Discoverers;

public abstract class RegexPluginDiscoverer : IPluginDiscoverer
{
    protected IEnumerable<string> InstallationPaths => _installationPaths ??=
        InstallationPathsImpl.Select(Path.TrimEndingDirectorySeparator);
    IEnumerable<string>? _installationPaths;
    protected abstract IEnumerable<string> InstallationPathsImpl { get; }
    protected abstract string ParentDirectoryRegex { get; }
    protected abstract string ExecutableRegex { get; }

    readonly Regex RegexParentDirectory, RegexExecutable;

    public RegexPluginDiscoverer()
    {
        RegexParentDirectory = new Regex(ParentDirectoryRegex, RegexOptions.Compiled);
        RegexExecutable = new Regex(ExecutableRegex, RegexOptions.Compiled);
    }

    public IEnumerable<Plugin> Discover()
    {
        var directories = InstallationPaths
            .Where(Directory.Exists)
            .SelectMany(installationPath =>
                Directory.EnumerateDirectories(installationPath)
                    .Where(dir => RegexParentDirectory.IsMatch(Path.GetDirectoryName(dir)!))
            );

        if (Environment.OSVersion.Platform == PlatformID.Unix)
            directories = directories.Append("/bin");

        return directories
            .SelectMany(pluginDirectory =>
                Directory.EnumerateFiles(pluginDirectory)
                    .Where(file => RegexExecutable.IsMatch(Path.GetFileName(file)))
            )
            .Select(GetDiscoveredPlugin)
            // skip same versions unless it's 'Unknown'
            .DistinctBy(plugin => plugin.Version == "Unknown" ? Guid.NewGuid().ToString() : plugin.Version);
    }

    protected abstract Plugin GetDiscoveredPlugin(string executablePath);
}
