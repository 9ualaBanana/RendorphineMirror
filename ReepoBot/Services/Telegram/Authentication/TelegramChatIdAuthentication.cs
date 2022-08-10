using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Authentication;

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

    internal TelegramAuthenticationToken? GetTokenFor(ChatId id) => IsAuthenticated(id) ? _authenticatedUsers[id] : null;

    internal async Task AuthenticateAsync(Message message)
    {
        if (IsAuthenticated(message.Chat.Id))
        { await _bot.TrySendMessageAsync(message.Chat.Id, "You are already authenticated.", _logger); return; }

        await TryAuthenticateAsyncFrom(message);
    }

    async Task TryAuthenticateAsyncFrom(Message message)
    {
        if (TelegramCredentials.TryParse(message, out var credentials))
        {
            if (await TryAuthenticateAsync(credentials!))
                _ = _bot.TrySendMessageAsync(message.Chat.Id, "You are successfully authenticated.", _logger);
            else
                _ = _bot.TrySendMessageAsync(message.Chat.Id, "Wrong credentials.", _logger);
        }
        else
        { _ = _bot.TrySendMessageAsync(message.Chat.Id, "Credentials are in a wrong format.", _logger); return; }
    }

    async Task<bool> TryAuthenticateAsync(TelegramCredentials credentials)
    {
        try
        {
            var sessionId = await AuthenticateAsync(credentials); _authenticatedUsers.Add(credentials.Id, new(sessionId));
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

    internal void LogOut(ChatId id)
    {
        if (!IsAuthenticated(id))
        { _ = _bot.TrySendMessageAsync(id, "You are not authenticated.", _logger); }
        else
        { _authenticatedUsers.Remove(id); _ = _bot.TrySendMessageAsync(id, "You are successfully logged out.", _logger); }
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
