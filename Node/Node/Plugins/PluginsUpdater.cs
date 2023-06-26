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

    public PluginsUpdater(PluginManager pluginManager)
    {
        PluginManager = pluginManager;
    }

    async Task TryDeployUninstalledPluginsAsync(HttpResponseMessage response)
    {
        try { await DeployUninstalledPluginsAsync(response); }
        catch (Exception ex) { _logger.Error(ex, "{Service} was unable to deploy uninstalled plugins", "UserSettingsManager"); }
    }

    async Task DeployUninstalledPluginsAsync(HttpResponseMessage response)
    {
        var settings = (await Api.GetJsonIfSuccessfulAsync(response))["settings"].ThrowIfNull().ToObject<NodeCommon.Apis.ServerUserSettings>().ThrowIfNull().ToSettings();

        await trydeploy(PluginManager, settings.InstallSoftware);
        await trydeploy(PluginManager, settings.GetNodeInstallSoftware(Settings.Guid));


        async Task trydeploy(PluginManager pluginManager, UUserSettings.TMServerSoftware? software)
        {
            if (software is null) return;

            var installed = await pluginManager.GetInstalledPluginsAsync();
            var newcount = await PluginDeployer2.DeployUninstalledAsync(PluginChecker.GetInstallationTree(UUserSettings.ToDeploy(software), NodeGlobalState.Instance.Software.Value), installed);

            if (newcount != 0)
                await pluginManager.RediscoverPluginsAsync();
        }
    }
    #endregion
}
