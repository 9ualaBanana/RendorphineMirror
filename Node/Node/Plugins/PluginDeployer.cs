namespace Node.Plugins;

public static class PluginDeployer
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static async Task<bool> TryDeployUninstalledPluginsAsync(UserSettings userSettings, IReadOnlyCollection<Plugin> installedPlugins)
    {
        var hadUninstalled = false;

        Logger.Trace("Trying to deploy uninstalled plugins from {List}", nameof(userSettings.NodeInstallSoftware));
        hadUninstalled |= await DeployUninstalledPluginsAsync(userSettings.ThisNodeInstallSoftware, installedPlugins);
        Logger.Trace("Trying to deploy uninstalled plugins from {List}", nameof(userSettings.InstallSoftware));
        hadUninstalled |= await DeployUninstalledPluginsAsync(userSettings.InstallSoftware, installedPlugins);

        return hadUninstalled;
    }

    static async Task<bool> DeployUninstalledPluginsAsync(IEnumerable<PluginToDeploy> plugins, IReadOnlyCollection<Plugin> installedPlugins)
    {
        var hadUninstalled = false;

        foreach (var plugin in LeaveOnlyUninstalled(plugins, installedPlugins))
        {
            hadUninstalled = true;
            await DeployUninstalledPluginAsync(plugin);
        }

        return hadUninstalled;


        static IEnumerable<PluginToDeploy> LeaveOnlyUninstalled(IEnumerable<PluginToDeploy> plugins, IReadOnlyCollection<Plugin> installedPlugins)
        {
            return plugins.SelectMany(plugin => plugin.SelfAndSubPlugins)
                .Where(plugin => !installedPlugins.Any(installedPlugin => plugin == installedPlugin));
        }
    }

    static async Task DeployUninstalledPluginAsync(PluginToDeploy plugin)
    {
        Logger.Info("Deploying {PluginType} plugin", plugin.Type);
        await new ScriptPluginDeploymentInfo(plugin).DeployAsync();
        Logger.Info("{PluginType} plugin is deployed", plugin.Type);
    }
}
