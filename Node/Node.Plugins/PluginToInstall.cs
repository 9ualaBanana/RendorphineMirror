namespace Node.Plugins;

public record PluginToInstall(PluginType Type, PluginVersion Version, SoftwareVersionInfo.InstallationInfo? Installation);