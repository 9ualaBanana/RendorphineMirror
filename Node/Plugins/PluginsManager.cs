using Node.Plugins.Discoverers;
using Node.Plugins.Plugins;

namespace Node.Plugins;

// * Starts in background when node is being launched.
internal class PluginsManager
{
    // Delegates tasks to them and manages their properties.
    readonly IEnumerable<Plugin> _plugins = new HashSet<Plugin>(_pluginsCount);
    static HashSet<PluginDiscoverer> _pluginsDiscoverers = new(_pluginsCount);
    readonly static int _pluginsCount = typeof(PluginType).GetFields().Length - 1;

    internal PluginsManager(IEnumerable<Plugin> plugins)
    {
        _plugins = plugins;
    }

    #region Discovering
    internal static Dictionary<PluginType, Plugin> DiscoverInstalledPlugins()
    {
        return new(_pluginsDiscoverers
            .SelectMany(pluginDiscoverer => pluginDiscoverer.Discover())
            .Select(installedPlugin => new KeyValuePair<PluginType, Plugin>(installedPlugin.Type, installedPlugin))
            );
    }

    internal static void RegisterPlugin(Plugin plugin) => RegisterPluginDiscoverer(plugin.Discoverer);

    internal static void RegisterPluginDiscoverer(PluginDiscoverer pluginDiscoverer) => _pluginsDiscoverers.Add(pluginDiscoverer);
    #endregion
}
