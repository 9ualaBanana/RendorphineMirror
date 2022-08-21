using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Telegram.Updates;

public class TelegramUpdateHandler
{
    readonly ILogger _logger;

    readonly TelegramMessageHandler _messageHandler;
    readonly TelegramCallbackQueryHandler _callbackQueryHandler;
    readonly TelegramChatMemberUpdatedHandler _myChatMemberHandler;

    public TelegramUpdateHandler(
        ILogger<TelegramUpdateHandler> logger,
        TelegramMessageHandler messageHandler,
        TelegramCallbackQueryHandler callbackHandler,
        TelegramChatMemberUpdatedHandler myChatMemberHandler)
    {
        _logger = logger;
        _messageHandler = messageHandler;
        _callbackQueryHandler = callbackHandler;
        _myChatMemberHandler = myChatMemberHandler;
    }

    public async Task HandleAsync(Update update)
    {
        _logger.LogDebug("Update of {UpdateType} type is received", update.Type);
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
                _logger.LogWarning("No handler for update of {UpdateType} type is found", update.Type);
                return;
        }
        _logger.LogDebug("Update of {UpdateType} type is handled", update.Type);
    }
}
