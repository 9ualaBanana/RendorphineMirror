namespace Node.Plugins;

// TODO: non-static
public static partial class PluginChecker
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static IEnumerable<PluginToInstall> GetInstallationTree(IEnumerable<PluginToDeploy> plugins, IReadOnlyDictionary<string, SoftwareDefinition> software) =>
        plugins.SelectMany(p => GetInstallationTree(p.Type, p.Version, software)).Distinct();

    public static IEnumerable<PluginToInstall> GetInstallationTree(string type, PluginVersion version, IReadOnlyDictionary<string, SoftwareDefinition> software)
    {
        if (!Enum.TryParse<PluginType>(type, out var ptype))
        {
            Logger.Warn($"Unknown plugin type {type}, skipping");
            return Enumerable.Empty<PluginToInstall>();
        }

        return GetInstallationTree(ptype, version, software);
    }
    public static IEnumerable<PluginToInstall> GetInstallationTree(PluginType type, PluginVersion version, IReadOnlyDictionary<string, SoftwareDefinition> software)
    {
        var versiondef = GetVersionDefinition(type, ref version, software);
        if (versiondef is null || string.IsNullOrEmpty(versiondef.InstallScript))
            return new[] { new PluginToInstall(type, version, null) };

        var parents = versiondef.Requirements.Parents;
        var newparents = parents.SelectMany(parent => GetInstallationTree(parent.Type, parent.Version, software));
        return newparents.Append(new PluginToInstall(type, version, versiondef.InstallScript));
    }

    /// <param name="version"> Plugin version or null if any </param>
    public static bool IsInstalled(PluginType type, PluginVersion version, IReadOnlyCollection<Plugin> installedPlugins) =>
        installedPlugins.Any(i => i.Type == type && (version.IsEmpty || i.Version == version));


    static SoftwareVersionDefinition? GetVersionDefinition(PluginType type, ref PluginVersion version, IReadOnlyDictionary<string, SoftwareDefinition> software)
    {
        var soft = software.GetValueOrDefault(type.ToString());
        if (soft is null) return null;

        if (version.IsEmpty)
        {
            if (soft.Versions.Count == 0)
                return null;

            version = soft.Versions.Keys.Max();
        }

        return soft.Versions[version];
    }
}
