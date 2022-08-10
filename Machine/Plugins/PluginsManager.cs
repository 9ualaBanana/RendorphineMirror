using Machine.Plugins.Deployment;
using Machine.Plugins.Discoverers;

namespace Machine.Plugins;

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
    public static async Task TryDeployUninstalledPluginsAsync(IEnumerable<PluginToDeploy> plugins, PluginsDeployer deployer)
    {
        _logger.Debug("Trying to deploy new plugins");

        foreach (var plugin in LeaveOnlyUninstalled(plugins))
            await TryDeployUninstalledPluginAsync(plugin, deployer);
    }

    public static async Task TryDeployUninstalledPluginAsync(PluginToDeploy plugin, PluginsDeployer deployer)
    {
        _logger.Info("Deploying new plugin: {PluginType}", plugin.Type);

        try { await DeployUninstalledPluginAsync(plugin, deployer); }
        catch (Exception ex) { _logger.Error(ex, "New plugin couldn't be deployed"); }
    }

    public static async Task DeployUninstalledPluginAsync(PluginToDeploy plugin, PluginsDeployer deployer)
    {
        await deployer.DeployAsync(plugin.GetDeploymentInfo());
        _logger.Info("New plugin is deployed");
        await DiscoverInstalledPluginsInBackground();
        _logger.Info("List of installed plugins is updated");
    }
    #endregion


    #region Discovering
    public static async Task<HashSet<Plugin>> DiscoverInstalledPluginsInBackground() =>
        await Task.Run(DiscoverInstalledPlugins);

    public static HashSet<Plugin> DiscoverInstalledPlugins()
    {
        return InstalledPlugins = _pluginsDiscoverers.SelectMany(pluginDiscoverer => pluginDiscoverer.Discover()).ToHashSet();
    }

    public static void RegisterPluginDiscoverers(params PluginDiscoverer[] pluginDiscoverers) => _pluginsDiscoverers.UnionWith(pluginDiscoverers);
    public static void RegisterPluginDiscoverer(PluginDiscoverer pluginDiscoverer) => _pluginsDiscoverers.Add(pluginDiscoverer);
    #endregion


    static IEnumerable<PluginToDeploy> LeaveOnlyUninstalled(IEnumerable<PluginToDeploy> plugins) =>
    plugins.SelectMany(plugin => plugin.SelfAndSubPlugins)
        .Where(plugin => !InstalledPlugins.Any(installedPlugin => plugin == installedPlugin));
}
