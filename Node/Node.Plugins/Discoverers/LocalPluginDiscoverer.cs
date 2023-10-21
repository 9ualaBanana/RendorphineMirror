namespace Node.Plugins.Discoverers;

/// <summary>
/// <see cref="PluginDiscoverer"/> for plugins in plugins/ directory.
/// Plugins must be versioned, e.g. plugins/esrgan/0.2/
/// </summary>
public abstract class LocalPluginDiscoverer : PluginDiscoverer
{
    protected sealed override IEnumerable<string> InstallationPathsImpl => new[] { PluginDirs.Directory };
    protected sealed override string? ParentDirectoryRegex => null;
    protected sealed override string ParentDirectoryPattern => PluginType.ToString().ToLowerInvariant();

    public required PluginDirs PluginDirs { get; init; }

    protected override IEnumerable<string> GetPossiblePluginDirectories()
    {
        string? dir = null;
        if (Directory.Exists(PluginDirs.Directory))
            dir = Directory.GetDirectories(PluginDirs.Directory, ParentDirectoryPattern, SearchOption.TopDirectoryOnly).SingleOrDefault();
        if (dir is null) return Enumerable.Empty<string>();

        return Directory.GetDirectories(dir);
    }

    protected override Plugin GetDiscoveredPlugin(string executablePath) => new LocalPlugin(PluginType, DetermineVersion(executablePath), executablePath);
    protected sealed override string DetermineVersion(string exepath) =>
        JsonConvert.DeserializeObject<SoftwareVersionInfo>(File.ReadAllText(Path.Combine(exepath, "..", "plugin.json"))).ThrowIfNull().Version;
}
