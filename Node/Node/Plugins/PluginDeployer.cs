namespace Node.Plugins;

public static class PluginDeployer
{
    static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static async Task TryDeployUninstalledPluginsAsync(UserSettings userSettings, IReadOnlyCollection<Plugin> installedPlugins)
    {
        Logger.Trace("Trying to deploy uninstalled plugins from {List}", nameof(userSettings.NodeInstallSoftware));
        await DeployUninstalledPluginsAsync(userSettings.ThisNodeInstallSoftware, installedPlugins);
        Logger.Trace("Trying to deploy uninstalled plugins from {List}", nameof(userSettings.InstallSoftware));
        await DeployUninstalledPluginsAsync(userSettings.InstallSoftware, installedPlugins);
    }

    static async Task DeployUninstalledPluginsAsync(IEnumerable<PluginToDeploy> plugins, IReadOnlyCollection<Plugin> installedPlugins)
    {
        foreach (var plugin in LeaveOnlyUninstalled(plugins, installedPlugins))
            await DeployUninstalledPluginAsync(plugin);


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
