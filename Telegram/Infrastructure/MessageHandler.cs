using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram.Infrastructure;

public abstract class MessageHandler : UpdateHandler
{
    /// <summary>
    /// Unique identifier for the chat where the <see cref="Bot.Types.Message"/> being handled came from.
    /// </summary>
    protected long ChatId => Message.Chat.Id;

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
