namespace Node.Plugins;

public record PluginToInstall(PluginType Type, PluginVersion Version, SoftwareInstallation? Installation);