using Machine.Plugins.Discoverers;

namespace Machine.Plugins;

public class PluginsManager
{
    // Delegates tasks to them and manages their properties.
    readonly IEnumerable<Plugin> _plugins = new HashSet<Plugin>(_pluginsTypesCount);
    readonly static HashSet<IPluginDiscoverer> _pluginsDiscoverers = new(_pluginsTypesCount);
    readonly static int _pluginsTypesCount = typeof(PluginType).GetFields().Length - 1;

    public PluginsManager(IEnumerable<Plugin> plugins)
    {
        _plugins = plugins;
    }

    #region Discovering
    internal static async Task<Plugin[]> DiscoverInstalledPluginsInBackground() =>
        await Task.Run(() => DiscoverInstalledPlugins());

    internal static Plugin[] DiscoverInstalledPlugins()
    {
        return _pluginsDiscoverers.SelectMany(pluginDiscoverer => pluginDiscoverer.Discover()).ToArray();
    }

    public static void RegisterPluginDiscoverers(params IPluginDiscoverer[] pluginDiscoverers) => _pluginsDiscoverers.UnionWith(pluginDiscoverers);
    public static void RegisterPluginDiscoverer(IPluginDiscoverer pluginDiscoverer) => _pluginsDiscoverers.Add(pluginDiscoverer);
    #endregion
}
