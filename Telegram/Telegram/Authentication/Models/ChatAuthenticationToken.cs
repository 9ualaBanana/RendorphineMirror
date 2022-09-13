using Telegram.Bot.Types;

namespace Telegram.Telegram.Authentication.Models;

public record ChatAuthenticationToken(ChatId ChatId, MPlusAuthenticationToken MPlus)
{
}
