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
            nameof(UserSettingsManager), nameof(IHeartbeatGenerator));

        if (_deploymentInProcess) return;

        _deploymentInProcess = true;
        await TryDeployUninstalledPluginsAsync(response);
        _deploymentInProcess = false;
    };

    async Task TryDeployUninstalledPluginsAsync(HttpResponseMessage response)
    {
        try { await DeployUninstalledPluginsAsync(response); }
        catch (Exception ex) { _logger.Error(ex, "{Service} was unable to deploy uninstalled plugins", nameof(UserSettingsManager)); }
    }

    static async Task DeployUninstalledPluginsAsync(HttpResponseMessage response)
    {
        var userSettings = await UserSettings.ReadOrThrowAsync(response);
        userSettings.Guid = Settings.Guid;
        await PluginsManager.TryDeployUninstalledPluginsAsync(userSettings);
    }
    #endregion
}
