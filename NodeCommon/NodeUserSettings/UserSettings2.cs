using Newtonsoft.Json;

namespace NodeCommon.NodeUserSettings;

/*
Actual settings.installsoftware schema:
{
    "python": {
        "3.11": {
            "plugins": {
                "python_esrgan": {
                    "version": "",
                    "subplugins": {
                        // i don't even remeber what next
                    }
                }
            }
        }
    }
}

But this is stupid, and since the server doesn't validate software names, we can use a more flattened schema:
(The server still expects there to be a "plugins" object on every software version, so we need to keep it)
{
    "python": {
        "3.10": { "plugins": {} },
        "3.11": { "plugins": {} }
    },
    "esrgan": {
        "1.0.0": { "plugins": {} }
    }
}
*/

/// <summary> User settings class that will eventually replace the current one </summary>
public class UserSettings2
{
    [JsonProperty("nodeinstallsoftware")] public RNodeInstallSoftware? NodeInstallSoftware { get; private set; }
    [JsonProperty("installsoftware")] public TMServerSoftware? InstallSoftware { get; private set; }

    public UserSettings2(RNodeInstallSoftware? nodeInstallSoftware, TMServerSoftware? installSoftware)
    {
        NodeInstallSoftware = nodeInstallSoftware;
        InstallSoftware = installSoftware;
    }

    public TMServerSoftware? GetNodeInstallSoftware(string guid) => NodeInstallSoftware?.GetValueOrDefault(guid);

    public void Install(string nodeguid, PluginType type, string? version)
    {
        NodeInstallSoftware ??= new();

        if (!NodeInstallSoftware.TryGetValue(nodeguid, out var soft))
            NodeInstallSoftware[nodeguid] = soft = new();

        Install(soft, type, version);
    }
    public void Install(PluginType type, string? version) => Install(InstallSoftware ??= new(), type, version);
    static void Install(TMServerSoftware software, PluginType type, string? version)
    {
        if (!software.TryGetValue(type.ToString().ToLowerInvariant(), out var softversions))
            software[type.ToString().ToLowerInvariant()] = softversions = new();

        version ??= string.Empty;
        if (!softversions.ContainsKey(version))
            softversions[version] = new();
    }

    public void UninstallAll(string nodeguid, PluginType type)
    {
        if (NodeInstallSoftware is null) return;

        if (NodeInstallSoftware.TryGetValue(nodeguid, out var soft))
            soft.Remove(type.ToString().ToLowerInvariant());
    }
    public void UninstallAll(PluginType type) => InstallSoftware?.Remove(type.ToString().ToLowerInvariant());

    public void Uninstall(string nodeguid, PluginType type, string? version)
    {
        if (NodeInstallSoftware is null) return;

        if (NodeInstallSoftware.TryGetValue(nodeguid, out var soft))
            Uninstall(soft, type, version);
    }
    public void Uninstall(PluginType type, string? version)
    {
        if (InstallSoftware is null) return;

        Uninstall(InstallSoftware, type, version);
    }
    static void Uninstall(TMServerSoftware software, PluginType type, string? version)
    {
        if (!software.TryGetValue(type.ToString().ToLowerInvariant(), out var versions)) return;

        versions.Remove(version ?? string.Empty);
        if (versions.Count == 0) software.Remove(type.ToString().ToLowerInvariant());
    }


    public class RNodeInstallSoftware : Dictionary<string, TMServerSoftware> { }        // <node GUID, ...>
    public class TMServerSoftware : Dictionary<string, TMServerSoftwareVersions> { }    // <plugin, ...>
    public class TMServerSoftwareVersions : Dictionary<string, UserSettingsSoft> { }    // <version, ...>
    public class UserSettingsSoft
    {
        [JsonProperty("plugins")] public readonly object Plugins = new();
    }
}