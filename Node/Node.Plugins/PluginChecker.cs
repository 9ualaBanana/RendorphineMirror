using SoftwareList = System.Collections.Immutable.ImmutableDictionary<Node.Plugins.Models.PluginType, System.Collections.Immutable.ImmutableDictionary<Node.Plugins.Models.PluginVersion, Node.Plugins.Models.SoftwareVersionInfo>>;

namespace Node.Plugins;

public static class PluginChecker
{
    public static IEnumerable<PluginToInstall> GetInstallationTree(SoftwareList software, IEnumerable<PluginToDeploy> plugins) =>
        plugins.SelectMany(p => GetInstallationTree(software, p.Type, p.Version)).Distinct();

    public static IEnumerable<PluginToInstall> GetInstallationTree(SoftwareList software, string type, PluginVersion version)
    {
        if (!Enum.TryParse<PluginType>(type, true, out var ptype))
            return Enumerable.Empty<PluginToInstall>();

        return GetInstallationTree(software, ptype, version);
    }
    public static IEnumerable<PluginToInstall> GetInstallationTree(SoftwareList software, PluginType type, PluginVersion version)
    {
        var versiondef = GetVersionDefinition(software, type, ref version);
        if (versiondef is null)
            return new[] { new PluginToInstall(type, version, null) };

        var parents = versiondef.Requirements.Parents;
        var newparents = parents.SelectMany(parent => GetInstallationTree(software, parent.Type, parent.Version));
        return newparents.Append(new PluginToInstall(type, version, versiondef.Installation));
    }

    static SoftwareVersionInfo? GetVersionDefinition(SoftwareList software, PluginType type, ref PluginVersion version)
    {
        var soft = software.GetValueOrDefault(type);
        if (soft is null) return null;

        if (version.IsEmpty)
        {
            if (soft.Count == 0)
                return null;

            version = soft.Keys.Max();
        }

        return soft[version];
    }
}
