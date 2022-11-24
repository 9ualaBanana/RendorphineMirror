using Newtonsoft.Json;

namespace NodeCommon.NodeUserSettings;

public class UserSettingsManager
{
    readonly static Logger _logger = LogManager.GetCurrentClassLogger();

    readonly HttpClient _httpClient;
    readonly CancellationToken _cancellationToken;
    readonly string _endpoint = $"{Api.TaskManagerEndpoint}/getmysettings";
    string BuildUrl(string? sessionId = default) => $"{_endpoint}?sessionid={sessionId ?? Settings.SessionId}";


    public UserSettingsManager(HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        _httpClient = httpClient;
        _cancellationToken = cancellationToken;
    }


    public async Task<UserSettings?> TryFetchAsync(string? sessionId = default)
    {
        try
        {
            var userSettings = await FetchAsync(sessionId, _cancellationToken); _logger.Debug("User settings were successfully fetched");
            return userSettings;
        }
        catch (Exception ex) { _logger.Error(ex, "Couldn't fetch user settings"); return null; }
    }

    public async Task<UserSettings> FetchAsync(string? sessionId = default, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(BuildUrl(sessionId), cancellationToken);
        return await UserSettings.ReadOrThrowAsync(response);
    }

    /// <inheritdoc cref="SetAsync(UserSettings, string?, CancellationToken)"/>
    public async Task<bool> TrySetAsync(UserSettings userSettings, string? sessionId = default, CancellationToken cancellationToken = default)
    {
        try { await SetAsync(userSettings, sessionId, cancellationToken); _logger.Debug("User settings were successfully set"); return true; }
        catch (Exception ex) { _logger.Error(ex, "Couldn't set user settings"); return false; }
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
        await Api.GetJsonFromResponseIfSuccessfulAsync(
            await _httpClient.PostAsync($"{Api.TaskManagerEndpoint}/setusersettings", httpContent, cancellationToken)
            );
    }
}
