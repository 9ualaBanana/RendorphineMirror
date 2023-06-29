namespace Node.Plugins;

public class PluginChecker
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    readonly ISoftwareListProvider SoftwareList;

    public PluginChecker(ISoftwareListProvider softwareList) => SoftwareList = softwareList;

    public IEnumerable<PluginToInstall> GetInstallationTree(IEnumerable<PluginToDeploy> plugins) =>
        plugins.SelectMany(p => GetInstallationTree(p.Type, p.Version)).Distinct();

    public IEnumerable<PluginToInstall> GetInstallationTree(string type, PluginVersion version)
    {
        if (!Enum.TryParse<PluginType>(type, out var ptype))
        {
            Logger.Warn($"Unknown plugin type {type}, skipping");
            return Enumerable.Empty<PluginToInstall>();
        }

        return GetInstallationTree(ptype, version);
    }
    public IEnumerable<PluginToInstall> GetInstallationTree(PluginType type, PluginVersion version)
    {
        var versiondef = GetVersionDefinition(type, ref version);
        if (versiondef is null || string.IsNullOrEmpty(versiondef.InstallScript))
            return new[] { new PluginToInstall(type, version, null) };

        var parents = versiondef.Requirements.Parents;
        var newparents = parents.SelectMany(parent => GetInstallationTree(parent.Type, parent.Version));
        return newparents.Append(new PluginToInstall(type, version, versiondef.InstallScript));
    }


    SoftwareVersionDefinition? GetVersionDefinition(PluginType type, ref PluginVersion version)
    {
        var soft = SoftwareList.Software.GetValueOrDefault(type.ToString());
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
