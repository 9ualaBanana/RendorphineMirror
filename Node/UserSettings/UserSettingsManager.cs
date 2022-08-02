using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Node.UserSettings;

internal class UserSettingsManager
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient _httpClient;

    internal UserSettingsManager(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    internal async Task<UserSettings?> TryFetchAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userSettings = await FetchAsync(cancellationToken); _logger.Debug("User settings were successfully fetched");
            return userSettings;
        }
        catch (Exception ex) { _logger.Error(ex, "Couldn't fetch user settings"); return null; }
    }

    internal async Task<UserSettings> FetchAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{Api.TaskManagerEndpoint}/getmysettings?sessionid={Settings.SessionId}", cancellationToken);
        var jToken = await Api.GetJsonFromResponseIfSuccessfulAsync(response);
        return ((JObject)jToken).Property("settings")!.Value.ToObject<UserSettings>()!;
    }

    internal async Task TrySetAsync(UserSettings userSettings, CancellationToken cancellationToken = default)
    {
        try { await SetAsync(userSettings, cancellationToken); _logger.Debug("User settings were successfully set"); }
        catch (Exception ex) { _logger.Error(ex, "Couldn't set user settings"); }
    }

    internal async Task SetAsync(UserSettings userSettings, CancellationToken cancellationToken = default)
    {
        var httpConent = new MultipartFormDataContent()
        {
            { new StringContent(Settings.SessionId!), "sessionid" },
            { new StringContent(JsonConvert.SerializeObject(userSettings)), "settings" }
        };
        (await _httpClient.PostAsync(
            $"{Api.TaskManagerEndpoint}/setusersettings", httpConent, cancellationToken)
            ).EnsureSuccessStatusCode();
    }
}
