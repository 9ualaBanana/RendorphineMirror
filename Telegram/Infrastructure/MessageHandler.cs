using Telegram.Bot.Types;
using Telegram.Infrastructure.Bot;

namespace Telegram.Infrastructure;

public abstract class MessageHandler : MessageHandler_, ISwitchableService<MessageHandler, Message>
{
    protected MessageHandler(
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
        : base(bot, httpContextAccessor, logger)
    {
    }

    public abstract bool Matches(Message message);
}
