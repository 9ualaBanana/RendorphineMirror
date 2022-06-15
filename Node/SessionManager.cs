using System.Text.Json;

namespace Node;

internal class SessionManager : IAsyncDisposable
{
    readonly HttpClient _httpClient;
    readonly Uri _api;
    readonly string _email;
    readonly string _password;
    string _sessionId = null!;

    internal SessionManager(string email, string password, string serverUri, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _api = new Uri(new Uri(serverUri), "rphaccounts");
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

        var response = await _httpClient.PostAsync(new Uri(_api, "login"), credentials);
        EnsureSuccessStatusCode(response);

        var responseJson = JsonDocument.Parse(response.Content.ReadAsStream()).RootElement;
        return _sessionId = responseJson.GetProperty("sessionid").GetString()!;
    }

    internal async Task<string> RequestNicknameAsync()
    {
        var sessionId = new FormUrlEncodedContent(
            new Dictionary<string, string>()
            {
                ["sessionid"] = _sessionId
            });
        var response = await _httpClient.PostAsync(new Uri(_api, "generatenickname"), sessionId);
        EnsureSuccessStatusCode(response);

        var responseJson = JsonDocument.Parse(response.Content.ReadAsStream()).RootElement;
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
        var response = await _httpClient.PostAsync(new Uri(_api, "logout"), sessionId);
        EnsureSuccessStatusCode(response);
    }

    static void EnsureSuccessStatusCode(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var responseJson = JsonDocument.Parse(response.Content.ReadAsStream()).RootElement;
        var responseStatusCode = responseJson.GetProperty("ok").GetInt32();
        if (responseStatusCode != 1)
            throw new HttpRequestException($"Couldn't login. Server responded with {responseStatusCode} status code");
    }

    public async ValueTask DisposeAsync()
    {
        await Logout();
    }
}
