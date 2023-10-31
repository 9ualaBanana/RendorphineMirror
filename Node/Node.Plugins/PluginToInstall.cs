namespace Node.Plugins;

public record PluginToInstall(PluginType Type, PluginVersion Version, bool IsLatest, SoftwareVersionInfo.InstallationInfo? Installation);
