using Machine.Plugins;
using Machine.Plugins.Deployment;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.UserSettings;

internal class UserSettingsManager : IHeartbeatGenerator
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly static Func<JToken, UserSettings> _deserializeUserSettings = jToken =>
        ((JObject)jToken).Property("settings")!.Value.ToObject<UserSettings>()!;

    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;


    #region IHeartbeatProducer
    public HttpRequestMessage Request => new(HttpMethod.Get, $"{Api.TaskManagerEndpoint}/getmysettings?sessionid={Settings.SessionId}");
    public EventHandler<HttpResponseMessage> ResponseHandler => async (_, response) =>
    {
        var jToken = await Api.GetJsonFromResponseIfSuccessfulAsync(response);
        var userSettings = _deserializeUserSettings(jToken);

        var pluginsDeployer = new PluginsDeployer(_httpClient, _cancellationToken);
        await PluginsManager.DeployUninstalledPluginsAsync(userSettings.NodeInstallSoftware, pluginsDeployer);
        await PluginsManager.DeployUninstalledPluginsAsync(userSettings.InstallSoftware, pluginsDeployer);
    };
    #endregion


    internal UserSettingsManager(HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;
    }


    internal async Task<UserSettings?> TryFetchAsync()
    {
        try
        {
            var userSettings = await FetchAsync(_cancellationToken); _logger.Debug("User settings were successfully fetched");
            return userSettings;
        }
        catch (Exception ex) { _logger.Error(ex, "Couldn't fetch user settings"); return null; }
    }

    internal async Task<UserSettings> FetchAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(Request.RequestUri, cancellationToken);
        var jToken = await Api.GetJsonFromResponseIfSuccessfulAsync(response);
        return _deserializeUserSettings(jToken);
    }

    internal async Task TrySetAsync(UserSettings userSettings, CancellationToken cancellationToken = default)
    {
        try { await SetAsync(userSettings, cancellationToken); _logger.Debug("User settings were successfully set"); }
        catch (Exception ex) { _logger.Error(ex, "Couldn't set user settings"); }
    }

    internal async Task SetAsync(UserSettings userSettings, CancellationToken cancellationToken = default)
    {
        var httpContent = new MultipartFormDataContent()
        {
            { new StringContent(Settings.SessionId!), "sessionid" },
            { new StringContent(JsonConvert.SerializeObject(userSettings)), "settings" }
        };
        (await _httpClient.PostAsync(
            $"{Api.TaskManagerEndpoint}/setusersettings", httpContent, cancellationToken)
            ).EnsureSuccessStatusCode();
    }
}
