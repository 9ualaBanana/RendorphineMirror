using Node.Plugins.Discoverers;
using Node.Plugins.Plugins;
using System.Net.Http.Json;

namespace Node.Plugins;

internal class PluginsManager
{
    // Delegates tasks to them and manages their properties.
    readonly IEnumerable<Plugin> _plugins = new HashSet<Plugin>(_pluginsTypesCount);
    readonly static HashSet<PluginDiscoverer> _pluginsDiscoverers = new(_pluginsTypesCount);
    readonly static int _pluginsTypesCount = typeof(PluginType).GetFields().Length - 1;

    internal PluginsManager(IEnumerable<Plugin> plugins)
    {
        _plugins = plugins;
    }

    internal async Task SendInstalledPluginsInfoAsync(string uri, HttpClient httpClient)
    {
        var jsonPayload = new Dictionary<string, object>()
        {
            ["nodeinfo"] = await MachineInfo.AsDTOAsync(),
            ["plugins"] = _plugins,
        };

        var response = await httpClient.PostAsync(uri, JsonContent.Create(jsonPayload));
        response.EnsureSuccessStatusCode();
    }

    #region Discovering
    internal static async Task<IEnumerable<Plugin>> DiscoverInstalledPluginsInBackground() =>
        await Task.Run(() => DiscoverInstalledPlugins());

    internal static IEnumerable<Plugin> DiscoverInstalledPlugins()
    {
        return _pluginsDiscoverers.SelectMany(pluginDiscoverer => pluginDiscoverer.Discover());
    }

    internal static void RegisterPluginDiscoverers(params PluginDiscoverer[] pluginDiscoverers) => _pluginsDiscoverers.UnionWith(pluginDiscoverers);
    internal static void RegisterPluginDiscoverer(PluginDiscoverer pluginDiscoverer) => _pluginsDiscoverers.Add(pluginDiscoverer);
    #endregion
}
