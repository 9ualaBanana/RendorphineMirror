namespace Node.Plugins.Deployment;

public record ScriptPluginDeploymentInfo(PluginToDeploy Plugin) : PluginDeploymentInfo
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    public override async Task DeployAsync(bool deleteInstaller = true)
    {
        var software = (await SoftwareRegistry.GetSoftware()).ThrowIfError();
        var flat = SoftwareDefinition.Flatten(software).ToImmutableDictionary(x => x.TypeName.ToLowerInvariant());

        foreach (var plugin in Plugin.SelfAndSubPlugins)
        {
            _logger.Info($"Installing {plugin.Type} {plugin.Version}");

            var soft = flat[plugin.Type.ToString().ToLowerInvariant()];
            var script = (string.IsNullOrWhiteSpace(Plugin.Version) ? soft.Versions.Last() : soft.Versions.First(x => x.Version == Plugin.Version)).InstallScript;

            PowerShellInvoker.Invoke(script);
        }
    }
}
