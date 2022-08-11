using System.Text.RegularExpressions;

namespace Machine.Plugins.Discoverers;

public abstract class PluginDiscoverer
{
    protected IEnumerable<string> InstallationPaths => _installationPaths ??=
        InstallationPathsImpl.Select(Path.TrimEndingDirectorySeparator);
    IEnumerable<string>? _installationPaths;
    protected abstract IEnumerable<string> InstallationPathsImpl { get; }

    protected virtual string ParentDirectoryPattern => "*";
    protected virtual string ExecutableName => "*";
    protected virtual string? ParentDirectoryRegex => null;
    protected virtual string? ExecutableRegex => null;
    readonly Regex? RegexParentDirectory, RegexExecutable;

    public PluginDiscoverer()
    {
        RegexParentDirectory = ParentDirectoryRegex is null ? null : new Regex(ParentDirectoryRegex, RegexOptions.Compiled);
        RegexExecutable = ExecutableRegex is null ? null : new Regex(ExecutableRegex, RegexOptions.Compiled);
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
                .Append(installationPath)
                .Where(dir => RegexParentDirectory?.IsMatch(Path.GetFileName(dir)!) ?? true)
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
                .Where(file => RegexExecutable?.IsMatch(Path.GetFileName(file)) ?? true)
            )
            .Select(GetDiscoveredPlugin)
            // skip same versions unless it's unknown
            .DistinctBy(plugin => plugin.Version == "Unknown" ? Guid.NewGuid().ToString() : plugin.Version);
    }

    protected abstract Plugin GetDiscoveredPlugin(string executablePath);
}
