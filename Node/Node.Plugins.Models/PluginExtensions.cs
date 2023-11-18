namespace Node.Plugins.Models;

public static class PluginExtensions
{
    public static Plugin GetPlugin(this IEnumerable<Plugin> plugins, PluginType type) => plugins.GetPlugin(type, _ => true);
    public static Plugin? TryGetPlugin(this IEnumerable<Plugin> plugins, PluginType type) => plugins.TryGetPlugin(type, _ => true);

    public static Plugin GetPlugin(this IEnumerable<Plugin> plugins, PluginType type, Func<Plugin, bool> filter) => plugins.TryGetPlugin(type, filter).ThrowIfNull($"Plugin {type} is not installed");
    public static Plugin? TryGetPlugin(this IEnumerable<Plugin> plugins, PluginType type, Func<Plugin, bool> filter) => plugins.Where(p => p.Type == type).Where(filter).MaxBy(p => p.Version);
}
