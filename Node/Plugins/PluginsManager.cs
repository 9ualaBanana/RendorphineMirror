namespace Node.Plugins;

public static class PluginsManager
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly static HashSet<IPluginDiscoverer> _pluginsDiscoverers = new(_pluginsTypesCount);
    readonly static int _pluginsTypesCount = typeof(PluginType).GetFields().Length - 1;

    /*
    /// <remarks>
    /// Lazily evaluated. Might contain outdated info.
    /// Call <see cref="DiscoverInstalledPlugins"/> or <see cref="DiscoverInstalledPluginsInBackground"/> to update.
    /// </remarks>
    public static HashSet<Plugin> InstalledPlugins
    {
        get => _installedPlugins ??= DiscoverInstalledPluginsAsync();
        private set => _installedPlugins = value;
    }*/

    static HashSet<Plugin>? _installedPlugins;


    #region Deployment
    internal static async Task TryDeployUninstalledPluginsAsync(UserSettings userSettings)
    {
        _logger.Trace("Trying to deploy uninstalled plugins from {List}", nameof(userSettings.NodeInstallSoftware));
        await DeployUninstalledPluginsAsync(userSettings.ThisNodeInstallSoftware);
        _logger.Trace("Trying to deploy uninstalled plugins from {List}", nameof(userSettings.InstallSoftware));
        await DeployUninstalledPluginsAsync(userSettings.InstallSoftware);
    }

    static async Task DeployUninstalledPluginsAsync(IEnumerable<PluginToDeploy> plugins)
    {
        foreach (var plugin in await LeaveOnlyUninstalled(plugins))
            await DeployUninstalledPluginAsync(plugin);
    }

    static async Task DeployUninstalledPluginAsync(PluginToDeploy plugin)
    {
        _logger.Info("Deploying {PluginType} plugin", plugin.Type);
        await new ScriptPluginDeploymentInfo(plugin).DeployAsync();
        _logger.Info("{PluginType} plugin is deployed", plugin.Type);
        await DiscoverInstalledPluginsAsync();
    }
    #endregion


    #region Discovering
    public static IReadOnlyCollection<Plugin>? GetInstalledPluginsCache() => _installedPlugins;

    /// <summary> Discovers installed plugins. Returns already cached result if available. </summary>
    public static async ValueTask<HashSet<Plugin>> GetInstalledPluginsAsync() => _installedPlugins ??= await DiscoverInstalledPluginsAsync();

    public static async ValueTask<HashSet<Plugin>> DiscoverInstalledPluginsAsync()
    {
        _installedPlugins = (await Task.WhenAll(_pluginsDiscoverers.Select(async d => { try { return await d.Discover(); } catch { return Enumerable.Empty<Plugin>(); } }))).SelectMany(ps => ps).ToHashSet();
        _logger.Info("List of installed plugins is updated");

        NodeGlobalState.Instance.InstalledPlugins.SetRange(_installedPlugins);
        return _installedPlugins;
    }

    public static void RegisterPluginDiscoverers(params IPluginDiscoverer[] pluginDiscoverers) => _pluginsDiscoverers.UnionWith(pluginDiscoverers);
    public static void RegisterPluginDiscoverer(IPluginDiscoverer pluginDiscoverer) => _pluginsDiscoverers.Add(pluginDiscoverer);
    #endregion


    static async ValueTask<IEnumerable<PluginToDeploy>> LeaveOnlyUninstalled(IEnumerable<PluginToDeploy> plugins)
    {
        var installed = await GetInstalledPluginsAsync();

        return plugins.SelectMany(plugin => plugin.SelfAndSubPlugins)
            .Where(plugin => !installed.Any(installedPlugin => plugin == installedPlugin));
    }
}
