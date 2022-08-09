using System.Text.RegularExpressions;

namespace Machine.Plugins.Discoverers;

public abstract class PluginDiscoverer : IPluginDiscoverer
{
    protected IEnumerable<string> InstallationPaths => _installationPaths ??=
        InstallationPathsImpl.Select(Path.TrimEndingDirectorySeparator);
    IEnumerable<string>? _installationPaths;
    protected abstract IEnumerable<string> InstallationPathsImpl { get; }

    protected virtual string ParentDirectoryPattern => "*";
    protected virtual string ExecutableName => "*";
    protected virtual string ParentDirectoryRegex => ".*";
    protected virtual string ExecutableRegex => ".*";
    readonly Regex RegexParentDirectory, RegexExecutable;

    public PluginDiscoverer()
    {
        RegexParentDirectory = new Regex(ParentDirectoryRegex, RegexOptions.Compiled);
        RegexExecutable = new Regex(ExecutableRegex, RegexOptions.Compiled);
    }

    public IEnumerable<Plugin> Discover()
    {
        var directories = InstallationPaths
            .Where(Directory.Exists)
            .SelectMany(installationPath =>
                Directory.EnumerateDirectories(
                    installationPath,
                    ParentDirectoryPattern,
                    SearchOption.TopDirectoryOnly
                )
                .Where(dir => RegexParentDirectory.IsMatch(Path.GetFileName(dir)!))
            );

        if (Environment.OSVersion.Platform == PlatformID.Unix)
            directories = directories.Append("/bin");

        return directories
            .SelectMany(pluginDirectory =>
                Directory.EnumerateFiles(
                    pluginDirectory,
                    ExecutableName,
                    SearchOption.TopDirectoryOnly
                )
                .Where(file => RegexExecutable.IsMatch(Path.GetFileName(file)))
            )
            .Select(GetDiscoveredPlugin)
            // skip same versions unless it's unknown
            .DistinctBy(plugin => plugin.Version == "Unknown" ? Guid.NewGuid().ToString() : plugin.Version);
    }

    protected abstract Plugin GetDiscoveredPlugin(string executablePath);
}
