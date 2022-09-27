using Telegram.Bot.Types;
using Telegram.Telegram.Updates;

namespace Telegram.Telegram;

public abstract class TelegramCallbackQueryHandlerBase : TelegramUpdateHandler
{
    protected long ChatIdFrom(Update update) => update.CallbackQuery!.Message!.Chat.Id;
    protected string CallbackDataFrom(Update update) => update.CallbackQuery!.Data!;


    protected TelegramCallbackQueryHandlerBase(ILogger logger, TelegramBot bot) : base(logger, bot)
    {
    }
}
