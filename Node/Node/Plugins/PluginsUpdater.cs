using Node.Heartbeat;

namespace Node.Plugins;

internal class PluginsUpdater : IHeartbeatGenerator
{
    readonly static ILogger _logger = LogManager.GetCurrentClassLogger();

    readonly string _endpoint = $"{Api.TaskManagerEndpoint}/getmysettings";
    string BuildUrl(string? sessionId = default) => $"{_endpoint}?sessionid={sessionId ?? Settings.SessionId}";



    #region IHeartbeatGenerator
    public HttpRequestMessage Request => new(HttpMethod.Get, BuildUrl());
    public HttpContent? Content => null;
    bool _deploymentInProcess = false;
    public EventHandler<HttpResponseMessage> ResponseHandler => async (_, response) =>
    {
        _logger.Trace("{Service}'s (as {Interface}) plugins deployment callback is called",
            "UserSettingsManager", nameof(IHeartbeatGenerator));

        if (_deploymentInProcess) return;

        _deploymentInProcess = true;
        await TryDeployUninstalledPluginsAsync(response);
        _deploymentInProcess = false;
    };

    readonly PluginManager PluginManager;
    readonly PluginChecker PluginChecker;
    readonly PluginDeployer PluginDeployer;

    public PluginsUpdater(PluginManager pluginManager, PluginChecker pluginChecker, PluginDeployer pluginDeployer)
    {
        PluginManager = pluginManager;
        PluginChecker = pluginChecker;
        PluginDeployer = pluginDeployer;
    }

    async Task TryDeployUninstalledPluginsAsync(HttpResponseMessage response)
    {
        try { await DeployUninstalledPluginsAsync(response); }
        catch (Exception ex) { _logger.Error(ex, "{Service} was unable to deploy uninstalled plugins", "UserSettingsManager"); }
    }

    async Task DeployUninstalledPluginsAsync(HttpResponseMessage response)
    {
        var settings = (await response.GetJsonIfSuccessfulAsync())["settings"].ThrowIfNull().ToObject<NodeCommon.Apis.ServerUserSettings>().ThrowIfNull().ToSettings();

        await trydeploy(settings.InstallSoftware);
        await trydeploy(settings.GetNodeInstallSoftware(Settings.Guid));


        async Task trydeploy(UUserSettings.TMServerSoftware? software)
        {
            if (software is null) return;

            var newcount = PluginDeployer.DeployUninstalled(PluginChecker.GetInstallationTree(UUserSettings.ToDeploy(software)));
            if (newcount != 0)
                await PluginManager.RediscoverPluginsAsync();
        }
    }
    #endregion
}
