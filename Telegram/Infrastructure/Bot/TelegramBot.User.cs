using System.Security.Claims;
using Telegram.Bot.Types;

namespace Telegram.Infrastructure.Bot;

public partial class TelegramBot
{
    /// <summary>
    /// Represents <see cref="ClaimsPrincipal"/> that belong to a Telegram bot user uniquely identified by <see cref="ChatId"/>.
    /// </summary>
    internal record User(ChatId ChatId, ClaimsPrincipal _)
    {
    }
}
