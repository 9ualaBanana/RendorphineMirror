namespace Node.Plugins.Discoverers;

/// <summary>
/// <see cref="PluginDiscoverer"/> for plugins in plugins/ directory.
/// Plugins must be versioned, e.g. plugins/esrgan/0.2/
/// </summary>
public abstract class LocalPluginDiscoverer : PluginDiscoverer
{
    protected sealed override IEnumerable<string> InstallationPathsImpl => new[] { "plugins" };
    protected sealed override string? ExecutableRegex => null;
    protected sealed override string? ParentDirectoryRegex => null;

    protected override abstract string ParentDirectoryPattern { get; }
    protected override abstract string ExecutableName { get; }

    protected override IEnumerable<string> GetPossiblePluginDirectories()
    {
        string? dir = null;
        if (Directory.Exists("plugins"))
            dir = Directory.GetDirectories("plugins", ParentDirectoryPattern, SearchOption.TopDirectoryOnly).SingleOrDefault();
        if (dir is null) return Enumerable.Empty<string>();

        return Directory.GetDirectories(dir);
    }

    protected override string DetermineVersion(string exepath) => Path.GetFileName(Path.GetDirectoryName(exepath)!);
}
