namespace Node.Plugins.Models;

public record Plugin(PluginType Type, PluginVersion Version, string Path);
public record LocalPlugin(PluginType Type, PluginVersion Version, string Path) : Plugin(Type, Version, Path);
