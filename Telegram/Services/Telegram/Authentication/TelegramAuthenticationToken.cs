using Telegram.Bot.Types;

namespace Telegram.Services.Telegram.Authentication;

public record TelegramAuthenticationToken(ChatId ChatId, string SessionId)
{
}
