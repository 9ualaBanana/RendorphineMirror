using System.Security.Claims;
using Telegram.Bot.Types;

namespace Telegram.Bot;

/// <summary>
/// Represents <see cref="ClaimsPrincipal"/> that belong to a Telegram bot user uniquely identified by <see cref="ChatId"/>.
/// </summary>
internal record TelegramBotUser(ChatId ChatId, ClaimsPrincipal User)
{
}
