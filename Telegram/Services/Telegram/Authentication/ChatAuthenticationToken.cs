using Telegram.Bot.Types;

namespace Telegram.Services.Telegram.Authentication;

public record ChatAuthenticationToken(ChatId ChatId, MPlusAuthenticationToken MPlus)
{
}
