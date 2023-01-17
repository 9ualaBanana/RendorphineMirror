using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Telegram.Updates;

public class TelegramUpdateTypeHandler : TelegramUpdateHandler
{
    readonly TelegramMessageHandler _messageHandler;
    readonly TelegramCallbackQueryHandler _callbackQueryHandler;
    readonly TelegramChatMemberUpdatedHandler _myChatMemberHandler;



    public TelegramUpdateTypeHandler(
        ILogger<TelegramUpdateTypeHandler> logger,
        TelegramBot bot,
        TelegramMessageHandler messageHandler,
        TelegramCallbackQueryHandler callbackHandler,
        TelegramChatMemberUpdatedHandler myChatMemberHandler) : base(logger, bot)
    {
        _messageHandler = messageHandler;
        _callbackQueryHandler = callbackHandler;
        _myChatMemberHandler = myChatMemberHandler;
    }



    public override async Task HandleAsync(Update update)
    {
        Logger.LogDebug("Update of {UpdateType} type is received", update.Type);
        switch (update.Type)
        {
            case UpdateType.Message:
                await _messageHandler.HandleAsync(update);
                break;
            case UpdateType.CallbackQuery:
                await _callbackQueryHandler.HandleAsync(update);
                break;
            case UpdateType.MyChatMember:
                await _myChatMemberHandler.HandleAsync(update);
                break;
            default:
                return;
        }
        Logger.LogDebug("Update of {UpdateType} type is handled", update.Type);
    }
}
