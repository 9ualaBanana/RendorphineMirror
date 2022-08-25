using Telegram.Bot.Types;

namespace Telegram.Services.Telegram.Updates;

public abstract class TelegramUpdateHandler
{
    protected readonly ILogger Logger;
    protected readonly TelegramBot Bot;



    public TelegramUpdateHandler(ILogger logger, TelegramBot bot)
    {
        Logger = logger;
        Bot = bot;
    }



    public abstract Task HandleAsync(Update update);
}
