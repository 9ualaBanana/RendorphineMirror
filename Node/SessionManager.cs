using System.Text.Json;

namespace Node;

internal class SessionManager : IAsyncDisposable
{
    readonly HttpClient _httpClient;
    readonly string _serverUri;
    string _AccountsEndpoint => $"{_serverUri}/rphaccounts";
    string _TaskManagerEndpoint => $"{_serverUri}/rphtaskmgr";
    readonly string _email;
    readonly string _password;
    string _sessionId = null!;

    internal SessionManager(string email, string password, string serverUri, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _serverUri = serverUri;
        _email = email;
        _password = password;
    }

    internal async Task<string> LoginAsync()
    {
        var credentials = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["email"] = _email,
                ["password"] = _password
            });

        JsonElement responseJson = GetJsonFromResponseIfSuccessful(
            await _httpClient.PostAsync($"{_AccountsEndpoint}/login", credentials)
            );

        return _sessionId = responseJson.GetProperty("sessionid").GetString()!;
    }

    internal async Task<string> RequestNicknameAsync()
    {
        var sessionId = new FormUrlEncodedContent(
            new Dictionary<string, string>()
            {
                ["sessionid"] = _sessionId
            });

        JsonElement responseJson = GetJsonFromResponseIfSuccessful(
            await _httpClient.PostAsync($"{_TaskManagerEndpoint}/generatenickname", sessionId)
            );

        return responseJson.GetProperty("nickname").GetString()!;
    }

    internal async Task Logout()
    {
        if (_sessionId is null) return;

        var sessionId = new FormUrlEncodedContent(
            new Dictionary<string, string>()
            {
                ["sessionid"] = _sessionId
            });

        GetJsonFromResponseIfSuccessful(
            await _httpClient.PostAsync($"{_AccountsEndpoint}/logout", sessionId)
            );
    }

    static JsonElement GetJsonFromResponseIfSuccessful(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var responseJson = JsonDocument.Parse(response.Content.ReadAsStream()).RootElement;
        var responseStatusCode = responseJson.GetProperty("ok").GetInt32();
        if (responseStatusCode != 1)
            throw new HttpRequestException($"Couldn't login. Server responded with {responseStatusCode} status code");

        return responseJson;
    }

    public async ValueTask DisposeAsync()
    {
        await Logout();
    }
}
