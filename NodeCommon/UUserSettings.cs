namespace NodeCommon;

public class UUserSettings
{
    public RNodeInstallSoftware NodeInstallSoftware { get; }
    public TMServerSoftware InstallSoftware { get; }

    public UUserSettings(RNodeInstallSoftware? nodeInstallSoftware, TMServerSoftware? installSoftware)
    {
        NodeInstallSoftware = nodeInstallSoftware ?? new();
        InstallSoftware = installSoftware ?? new();
    }

    public TMServerSoftware? GetNodeInstallSoftware(string guid) =>
        NodeInstallSoftware?.GetValueOrDefault(guid);

    public static IEnumerable<PluginToDeploy> ToDeploy(TMServerSoftware software) =>
        software.SelectMany(k => k.Value.Select(v => new PluginToDeploy(k.Key, v))).Distinct();

    /// <summary> Install a plugin to a specific node </summary>
    public void Install(string nodeguid, PluginType type, PluginVersion version)
    {
        if (!NodeInstallSoftware.TryGetValue(nodeguid, out var soft))
            NodeInstallSoftware[nodeguid] = soft = new();

        Install(soft, type, version);
    }
    /// <summary> Install a plugin to all nodes </summary>
    public void Install(PluginType type, PluginVersion version) => Install(InstallSoftware, type, version);
    static void Install(TMServerSoftware software, PluginType type, PluginVersion version)
    {
        if (!software.TryGetValue(type, out var softversions))
            software[type] = softversions = new();

        softversions.Add(version);
    }

    /// <summary> Remove a plugin with all versions from a node </summary>
    public void UninstallAll(string nodeguid, PluginType type)
    {
        if (NodeInstallSoftware is null) return;

        if (NodeInstallSoftware.TryGetValue(nodeguid, out var soft))
            soft.Remove(type);
    }
    /// <summary> Remove a plugin with all versions from all nodes </summary>
    public void UninstallAll(PluginType type) => InstallSoftware?.Remove(type);

    /// <summary> Remove a plugin version from a node </summary>
    public void Uninstall(string nodeguid, PluginType type, PluginVersion version)
    {
        if (NodeInstallSoftware is null) return;

        if (NodeInstallSoftware.TryGetValue(nodeguid, out var soft))
            Uninstall(soft, type, version);
    }
    /// <summary> Remove a plugin version from all nodes </summary>
    public void Uninstall(PluginType type, PluginVersion version)
    {
        if (InstallSoftware is null) return;

        Uninstall(InstallSoftware, type, version);
    }
    static void Uninstall(TMServerSoftware software, PluginType type, PluginVersion version)
    {
        if (!software.TryGetValue(type, out var versions)) return;

        versions.Remove(version);
        if (versions.Count == 0) software.Remove(type);
    }



    public class RNodeInstallSoftware : Dictionary<string, TMServerSoftware> { }        // <node GUID, ...>
    public class TMServerSoftware : Dictionary<PluginType, HashSet<PluginVersion>> { }         // <plugin, versions>
}