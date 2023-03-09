using System.Security.Claims;
using Telegram.Bot.Types;

namespace Telegram.Tasks;

internal record TelegramBotUser(ChatId ChatId, ClaimsPrincipal User)
{
}
