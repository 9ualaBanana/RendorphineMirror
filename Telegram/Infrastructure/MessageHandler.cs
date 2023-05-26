using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;

namespace Telegram.Infrastructure;

public abstract class MessageHandler : UpdateHandler
{
    /// <summary>
    /// Unique identifier for the chat where the <see cref="Bot.Types.Message"/> being handled came from.
    /// </summary>
    protected ChatId ChatId => Message.Chat.Id;

    /// <summary>
    /// The <see cref="Bot.Types.Message"/> being handled.
    /// </summary>
    protected virtual Message Message => Update.Message!;

    protected MessageHandler(
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
        : base(bot, httpContextAccessor, logger)
    {
    }
}
