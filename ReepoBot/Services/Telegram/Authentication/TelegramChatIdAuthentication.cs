using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Authentication;

public class TelegramChatIdAuthentication
{
    readonly ILogger _logger;

    readonly TelegramBot _bot;

    // Change to in-memory database.
    readonly Dictionary<string, string> _credentials = new()
    {
        ["admin"] = "admin"
    };
    readonly HashSet<ChatId> _authenticatedIds = new();

    public TelegramChatIdAuthentication(ILogger<TelegramChatIdAuthentication> logger, TelegramBot bot)
    {
        _logger = logger;
        _bot = bot;
    }

    internal void Required(Action action, ChatId id)
    {
        if (IsAuthenticated(id)) action();
        else { _ = _bot.TrySendMessageAsync(id, "Authentication required.", _logger); }
    }

    internal void Authenticate(Message message)
    {
        if (IsAuthenticated(message.Chat.Id))
        { _ = _bot.TrySendMessageAsync(message.Chat.Id, "You are already authenticated.", _logger); return; }

        TryAuthenticateFrom(message);
    }

    void TryAuthenticateFrom(Message message)
    {
        if (TelegramCredentials.TryParse(message, out var credentials))
        {
            if (TryAuthenticate(message.Chat.Id, credentials!))
                _ = _bot.TrySendMessageAsync(message.Chat.Id, "You are successfully authenticated.", _logger);
            else
                _ = _bot.TrySendMessageAsync(message.Chat.Id, "Wrong credentials.", _logger);
        }
        else
        { _ = _bot.TrySendMessageAsync(message.Chat.Id, "Credentials are in a wrong format.", _logger); return; }
    }

    bool TryAuthenticate(ChatId id, TelegramCredentials credentials)
    {
        return _credentials.TryGetValue(credentials.Login, out var storedPassword)
            && credentials.Password == storedPassword
            && _authenticatedIds.Add(id);
    }

    internal void LogOut(ChatId id)
    {
        if (!IsAuthenticated(id))
        { _ = _bot.TrySendMessageAsync(id, "You are not authenticated.", _logger); }
        else
        { _authenticatedIds.Remove(id); _ = _bot.TrySendMessageAsync(id, "You are successfully logged out.", _logger); }
    }

    bool IsAuthenticated(ChatId id) => _authenticatedIds.Contains(id);
}
