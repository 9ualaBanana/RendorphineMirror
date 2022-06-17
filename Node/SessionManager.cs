using System.Net.Sockets;
using System.Text.Json;

namespace Node;

internal class SessionManager : IAsyncDisposable
{
    readonly HttpClient _httpClient;
    const string _serverUri = "https://tasks.microstock.plus";
    string _AccountsEndpoint => $"{_serverUri}/rphaccounts";
    string _TaskManagerEndpoint => $"{_serverUri}/rphtaskmgr";
    string _sessionId { get => Settings.SessionId!; set => Settings.SessionId = value!; }

    internal SessionManager(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new();
    }

    internal async Task<string> LoginAsync(string email, string password)
    {
        var credentials = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["email"] = email,
                ["password"] = password,
            });

        JsonElement responseJson = GetJsonFromResponseIfSuccessful(
            await _httpClient.PostAsync($"{_AccountsEndpoint}/login", credentials)
            );

        return _sessionId = responseJson.GetProperty("sessionid").GetString()!;
    }

    internal async Task CheckSessionAsync(string sid)
    {
        var credentials = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["sessionid"] = sid,
            });

        JsonElement responseJson = GetJsonFromResponseIfSuccessful(
            await _httpClient.PostAsync($"{_AccountsEndpoint}/checksession", credentials)
            );
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
        {
            if (responseJson.TryGetProperty("errormessage", out var errmsgp) && errmsgp.GetString() is { } errmsg)
                throw new HttpRequestException(errmsg);

            if (responseJson.TryGetProperty("errorcode", out var errcodep) && errcodep.GetString() is { } errcode)
                throw new HttpRequestException($"Couldn't login. Server responded with {errcode} error code");

            throw new HttpRequestException($"Couldn't login. Server responded with {responseStatusCode} status code");
        }

        return responseJson;
    }

    public async ValueTask DisposeAsync()
    {
        await Logout();
    }


    internal async Task<OperationResult> AuthAsync(string email, string password)
    {
        var login = await Repeat(() => LoginAsync(email, password)).ConfigureAwait(false);
        if (!login) return login.GetResult();

        return await CheckAsync().ConfigureAwait(false);
    }
    internal async ValueTask<OperationResult> CheckAsync()
    {
        if (_sessionId is null) return OperationResult.Err();

        var check = await Repeat(async () => { await CheckSessionAsync(_sessionId).ConfigureAwait(false); return true; }).ConfigureAwait(false);
        if (check)
        {
            var uinfo = check.Value;
            if (Settings.NodeName is null)
            {
                var nickr = await Repeat(RequestNicknameAsync).ConfigureAwait(false);

                if (nickr) Settings.NodeName = nickr.Value;
                else Settings.NodeName = Guid.NewGuid().ToString();
            }
        }

        return check.GetResult();
    }
    async Task<OperationResult<T>> Repeat<T>(Func<Task<T>> func)
    {
        while (true)
        {
            try { return await func().ConfigureAwait(false); }
            catch (SocketException)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                continue;
            }
            catch (Exception ex) { return OperationResult.Err(ex); }
        }
    }
}
