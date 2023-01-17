using Telegram.Bot;
using Telegram.Bot.Types;

namespace Telegram.Telegram.Updates;

public abstract class TelegramCallbackQueryHandlerBase : TelegramUpdateHandler
{
    protected long ChatIdFrom(Update update) => update.CallbackQuery!.Message!.Chat.Id;
    protected string CallbackDataFrom(Update update) => update.CallbackQuery!.Data!;


    protected TelegramCallbackQueryHandlerBase(ILogger logger, TelegramBot bot) : base(logger, bot)
    {
    }
}
