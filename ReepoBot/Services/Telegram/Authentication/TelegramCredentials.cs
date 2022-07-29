using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Authentication;

internal record TelegramCredentials(ChatId Id, string Login, string Password)
{
    internal static bool TryParse(Message message, out TelegramCredentials? credentials)
    {
        try { credentials = Parse(message); return true; }
        catch (Exception) { credentials = null; return false; }
    }
    internal static TelegramCredentials Parse(Message message)
    {
        ChatId id = message.Chat.Id;
        var messageParts = message.Text!.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return new(id, messageParts[1], messageParts[2]);
    }
}
