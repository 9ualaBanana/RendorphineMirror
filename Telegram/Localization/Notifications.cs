using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;

namespace Telegram.Localization;

public abstract partial class Notifications
{
    protected TelegramBot Bot { get; }
    protected CallbackQuerySerializer Serializer { get; }
    protected ILogger Logger { get; }

    protected Notifications(TelegramBot bot, CallbackQuerySerializer serializer, ILogger logger)
    {
        Bot = bot;
        Serializer = serializer;
        Logger = logger;
    }
}
