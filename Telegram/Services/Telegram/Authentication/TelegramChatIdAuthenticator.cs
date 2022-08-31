using Common;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;

namespace Telegram.Services.Telegram.Authentication;

public class TelegramChatIdAuthenticator
{
    readonly ILogger _logger;

    readonly TelegramBot _bot;
    readonly HttpClient _httpClient;

    readonly Dictionary<ChatId, TelegramAuthenticationToken> _authenticatedUsers = new();



    public TelegramChatIdAuthenticator(ILogger<TelegramChatIdAuthenticator> logger, TelegramBot bot, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _bot = bot;
        _httpClient = httpClientFactory.CreateClient();
    }



    internal TelegramAuthenticationToken? TryGetTokenFor(ChatId id)
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
            var authenticatedUser = await AuthenticateAsync(credentials);
            _authenticatedUsers.Add(credentials.ChatId, new(credentials.ChatId, authenticatedUser.UserId, authenticatedUser.SessionId));
            _logger.LogDebug("User is authenticated: {Login}", credentials.Login);
            return true;
        }
        catch (HttpRequestException) { }
        catch (Exception ex) { _logger.LogError(ex, "Couldn't authenticate user due to unexpected error"); }
        return false;
    }

    async Task<MPlusAuthenticationToken> AuthenticateAsync(TelegramCredentials credentials)
    {
        var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["email"] = credentials.Login,
            ["password"] = credentials.Password,
            ["guid"] = Guid.NewGuid().ToString()
        });
        var response = await _httpClient.PostAsync("https://tasks.microstock.plus/rphtaskmgr/login", httpContent);
        return ((JObject)await Api.GetJsonFromResponseIfSuccessfulAsync(response)).ToObject<MPlusAuthenticationToken>()!;
    }

    internal async Task LogOutAsync(ChatId id)
    {
        if (!IsAuthenticated(id))
        { await _bot.TrySendMessageAsync(id, "You are not authenticated."); }
        else
        { _authenticatedUsers.Remove(id); await _bot.TrySendMessageAsync(id, "You are successfully logged out."); }
    }

    bool IsAuthenticated(ChatId id) => _authenticatedUsers.ContainsKey(id);
}
