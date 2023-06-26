namespace Node.Plugins;

public record PluginToInstall(PluginType Type, PluginVersion Version, string? InstallScript);