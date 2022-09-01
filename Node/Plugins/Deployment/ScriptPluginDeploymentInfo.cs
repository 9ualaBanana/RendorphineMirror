namespace Node.Plugins.Deployment;

public record ScriptPluginDeploymentInfo(PluginToDeploy Plugin) : PluginDeploymentInfo
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public override async Task DeployAsync(bool deleteInstaller = true)
    {
        var software = (await Apis.GetSoftwareAsync()).ThrowIfError();

        foreach (var plugin in Plugin.SelfAndSubPlugins)
        {
            _logger.Info($"Installing {plugin.Type} {plugin.Version}");

            var soft = software[plugin.Type.ToString().ToLowerInvariant()];
            var script = (string.IsNullOrWhiteSpace(Plugin.Version) ? soft.Versions.OrderBy(x => x.Key).Last().Value : soft.Versions[plugin.Version]).InstallScript;

            PowerShellInvoker.InstallPlugin(plugin, script, deleteInstaller);
        }
    }
}
