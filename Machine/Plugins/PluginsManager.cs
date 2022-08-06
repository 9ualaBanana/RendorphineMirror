using Machine.Plugins.Deployment;
using Machine.Plugins.Discoverers;

namespace Machine.Plugins;

public static class PluginsManager
{
    public static HashSet<Plugin> InstalledPlugins
    {
        get => _installedPlugins ??= DiscoverInstalledPlugins();
        private set => _installedPlugins = value;
    }
    static HashSet<Plugin>? _installedPlugins;
    readonly static HashSet<IPluginDiscoverer> _pluginsDiscoverers = new(_pluginsTypesCount);
    readonly static int _pluginsTypesCount = typeof(PluginType).GetFields().Length - 1;

    public static async Task DeployUninstalledPluginsAsync(IEnumerable<PluginToDeploy> plugins, PluginsDeployer deployer)
    {
        foreach (var plugin in LeaveOnlyUninstalled(plugins))
        {
            await deployer.DeployAsync(plugin.GetDeploymentInfo());
            await DiscoverInstalledPluginsInBackground();
        }
    }

    static IEnumerable<PluginToDeploy> LeaveOnlyUninstalled(IEnumerable<PluginToDeploy> plugins) =>
        plugins.SelectMany(plugin => plugin.SelfAndSubPlugins)
            .Where(plugin => !InstalledPlugins.Any(installedPlugin => plugin == installedPlugin));

    #region Discovering
    public static async Task<HashSet<Plugin>> DiscoverInstalledPluginsInBackground() =>
        await Task.Run(DiscoverInstalledPlugins);

    public static HashSet<Plugin> DiscoverInstalledPlugins()
    {
        return InstalledPlugins = _pluginsDiscoverers.SelectMany(pluginDiscoverer => pluginDiscoverer.Discover()).ToHashSet();
    }

    public static void RegisterPluginDiscoverers(params IPluginDiscoverer[] pluginDiscoverers) => _pluginsDiscoverers.UnionWith(pluginDiscoverers);
    public static void RegisterPluginDiscoverer(IPluginDiscoverer pluginDiscoverer) => _pluginsDiscoverers.Add(pluginDiscoverer);
    #endregion
}
