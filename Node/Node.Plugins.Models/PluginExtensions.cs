namespace Node.Plugins.Models;

public static class PluginExtensions
{
    public static Plugin GetPlugin(this IEnumerable<Plugin> plugins, PluginType type) => plugins.TryGetPlugin(type).ThrowIfNull($"Plugin {type} is not installed");
    public static Plugin? TryGetPlugin(this IEnumerable<Plugin> plugins, PluginType type) => plugins.Where(p => p.Type == type).MaxBy(p => p.Version);
}
