using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;

namespace Telegram.Services.Telegram.Authentication;

public class TelegramChatIdAuthentication
{
    readonly ILogger _logger;

    readonly TelegramBot _bot;
    readonly HttpClient _httpClient;

    readonly Dictionary<ChatId, TelegramAuthenticationToken> _authenticatedUsers = new();

    public TelegramChatIdAuthentication(ILogger<TelegramChatIdAuthentication> logger, TelegramBot bot, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _bot = bot;
        _httpClient = httpClientFactory.CreateClient();
    }

    internal TelegramAuthenticationToken? GetTokenFor(ChatId id)
    {
        if (IsAuthenticated(id)) return _authenticatedUsers[id];

        _ = _bot.TrySendMessageAsync(id, "Authentication required."); return null;
    }

    internal async Task AuthenticateAsync(Message message)
    {
        if (IsAuthenticated(message.Chat.Id))
        { await _bot.TrySendMessageAsync(message.Chat.Id, "You are already authenticated."); return; }

        await TryAuthenticateAsyncFrom(message);
    }

    async Task TryAuthenticateAsyncFrom(Message message)
    {
        if (TelegramCredentials.TryParse(message, out var credentials))
        {
            if (await TryAuthenticateAsync(credentials!))
                await _bot.TrySendMessageAsync(message.Chat.Id, "You are successfully authenticated.");
            else
                await _bot.TrySendMessageAsync(message.Chat.Id, "Wrong credentials.");
        }
        else
        { await _bot.TrySendMessageAsync(message.Chat.Id, "Credentials are in a wrong format."); return; }
    }

    async Task<bool> TryAuthenticateAsync(TelegramCredentials credentials)
    {
        try
        {
            var sessionId = await AuthenticateAsync(credentials); _authenticatedUsers.Add(credentials.ChatId, new(credentials.ChatId, sessionId));
            _logger.LogDebug("User is authenticated:\nLogin: {Login}\nPassword: {Password}", credentials.Login, credentials.Password);
            return true;
        }
        catch (HttpRequestException) { }
        catch (Exception ex) { _logger.LogError(ex, "Couldn't authenticate user due to unexpected error"); }
        return false;
    }

    async Task<string> AuthenticateAsync(TelegramCredentials credentials)
    {
        var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["email"] = credentials.Login,
            ["password"] = credentials.Password,
            ["guid"] = Guid.NewGuid().ToString()
        });
        var response = await _httpClient.PostAsync("https://tasks.microstock.plus/rphtaskmgr/login", httpContent);
        return (string)((JObject)await GetJsonFromResponseIfSuccessfulAsync(response)).Property("sessionid")!;
    }

    internal async Task LogOutAsync(ChatId id)
    {
        if (!IsAuthenticated(id))
        { await _bot.TrySendMessageAsync(id, "You are not authenticated."); }
        else
        { _authenticatedUsers.Remove(id); await _bot.TrySendMessageAsync(id, "You are successfully logged out."); }
    }

    bool IsAuthenticated(ChatId id) => _authenticatedUsers.ContainsKey(id);

    static async ValueTask<JToken> GetJsonFromResponseIfSuccessfulAsync(HttpResponseMessage response, string? errorDetails = null)
    {
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new JsonTextReader(new StreamReader(stream));
        var responseJson = JToken.Load(reader);
        var responseStatusCode = responseJson["ok"]?.Value<int>();
        if (responseStatusCode != 1)
        {
            if (responseJson["errormessage"]?.Value<string>() is { } errmsg)
                throw new HttpRequestException(errmsg);

            if (responseJson["errorcode"]?.Value<string>() is { } errcode)
                throw new HttpRequestException($"{errorDetails} Server responded with {errcode} error code");

            throw new HttpRequestException($"{errorDetails} Server responded with {responseStatusCode} status code");
        }

        return responseJson;
    }
}
