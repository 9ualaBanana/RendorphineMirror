using Machine.Plugins.Deployment;
using Newtonsoft.Json;

namespace Node.UserSettings;

public class UserSettingsManager : IHeartbeatGenerator
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;


    #region IHeartbeatProducer
    public HttpRequestMessage Request => new(HttpMethod.Get, $"{Api.TaskManagerEndpoint}/getmysettings?sessionid={Settings.SessionId}");
    bool _deploymentInProcess = false;
    public EventHandler<HttpResponseMessage> ResponseHandler => async (_, response) =>
    {
        _logger.Trace("{Service}'s plugins deployment callback is called", nameof(UserSettingsManager));

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

    async Task DeployUninstalledPluginsAsync(HttpResponseMessage response)
    {
        var userSettings = await UserSettings.ReadOrThrowAsync(response);
        await userSettings.TryDeployUninstalledPluginsAsync(new PluginsDeployer(_httpClient, _cancellationToken));
    }
    #endregion


    public UserSettingsManager(HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;
    }


    public async Task<UserSettings?> TryFetchAsync()
    {
        try
        {
            var userSettings = await FetchAsync(_cancellationToken); _logger.Debug("User settings were successfully fetched");
            return userSettings;
        }
        catch (Exception ex) { _logger.Error(ex, "Couldn't fetch user settings"); return null; }
    }

    public async Task<UserSettings> FetchAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(Request.RequestUri, cancellationToken);
        return await UserSettings.ReadOrThrowAsync(response);
    }

    /// <inheritdoc cref="SetAsync(UserSettings, string?, CancellationToken)"/>
    public async Task TrySetAsync(UserSettings userSettings, string? sessionId = default, CancellationToken cancellationToken = default)
    {
        try { await SetAsync(userSettings, sessionId, cancellationToken); _logger.Debug("User settings were successfully set"); }
        catch (Exception ex) { _logger.Error(ex, "Couldn't set user settings"); }
    }

    /// <remarks>
    /// When <paramref name="sessionId"/> is not specified, <see cref="Settings.SessionId"/> is used. 
    /// </remarks>
    public async Task SetAsync(UserSettings userSettings, string? sessionId = default, CancellationToken cancellationToken = default)
    {
        var httpContent = new MultipartFormDataContent()
        {
            { new StringContent(sessionId ?? Settings.SessionId!), "sessionid" },
            { new StringContent(JsonConvert.SerializeObject(userSettings)), "settings" }
        };
        (await _httpClient.PostAsync(
            $"{Api.TaskManagerEndpoint}/setusersettings", httpContent, cancellationToken)
            ).EnsureSuccessStatusCode();
    }
}
