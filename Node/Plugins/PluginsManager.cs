using Common.Plugins.Deployment;
using Node.Plugins.Discoverers;

namespace Node.Plugins;

public static class PluginsManager
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly static HashSet<PluginDiscoverer> _pluginsDiscoverers = new(_pluginsTypesCount);
    readonly static int _pluginsTypesCount = typeof(PluginType).GetFields().Length - 1;

    /// <remarks>
    /// Lazily evaluated. Might contain outdated info.
    /// Call <see cref="DiscoverInstalledPlugins"/> or <see cref="DiscoverInstalledPluginsInBackground"/> to update.
    /// </remarks>
    public static HashSet<Plugin> InstalledPlugins
    {
        get => _installedPlugins ??= DiscoverInstalledPlugins();
        private set => _installedPlugins = value;
    }
    static HashSet<Plugin>? _installedPlugins;


    #region Deployment
    public static async Task DeployUninstalledPluginsAsync(IEnumerable<PluginToDeploy> plugins)
    {
        foreach (var plugin in LeaveOnlyUninstalled(plugins))
            await DeployUninstalledPluginAsync(plugin);
    }

    public static async Task DeployUninstalledPluginAsync(PluginToDeploy plugin)
    {
        _logger.Info("Deploying {PluginType} plugin", plugin.Type);
        await new ScriptPluginDeploymentInfo(plugin).DeployAsync();
        _logger.Info("{PluginType} plugin is deployed", plugin.Type);
        await DiscoverInstalledPluginsInBackground();
    }
    #endregion


    #region Discovering
    public static async Task<HashSet<Plugin>> DiscoverInstalledPluginsInBackground() =>
        await Task.Run(DiscoverInstalledPlugins);

    public static HashSet<Plugin> DiscoverInstalledPlugins()
    {
        InstalledPlugins = _pluginsDiscoverers.SelectMany(pluginDiscoverer => pluginDiscoverer.Discover()).ToHashSet();
        _logger.Info("List of installed plugins is updated");
        NodeGlobalState.Instance.InstalledPlugins.SetRange(InstalledPlugins);
        return InstalledPlugins;
    }

    public static void RegisterPluginDiscoverers(params PluginDiscoverer[] pluginDiscoverers) => _pluginsDiscoverers.UnionWith(pluginDiscoverers);
    public static void RegisterPluginDiscoverer(PluginDiscoverer pluginDiscoverer) => _pluginsDiscoverers.Add(pluginDiscoverer);
    #endregion


    static IEnumerable<PluginToDeploy> LeaveOnlyUninstalled(IEnumerable<PluginToDeploy> plugins) =>
    plugins.SelectMany(plugin => plugin.SelfAndSubPlugins)
        .Where(plugin => !InstalledPlugins.Any(installedPlugin => plugin == installedPlugin));
}
