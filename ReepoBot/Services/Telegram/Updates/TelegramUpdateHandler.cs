using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ReepoBot.Services.Telegram.Updates;

public class TelegramUpdateHandler
{
    readonly ILogger _logger;

    readonly TelegramMessageHandler _messageHandler;
    readonly TelegramChatMemberUpdatedHandler _myChatMemberHandler;

    public TelegramUpdateHandler(
        ILogger<TelegramUpdateHandler> logger,
        TelegramMessageHandler messageHandler,
        TelegramChatMemberUpdatedHandler myChatMemberHandler)
    {
        _logger = logger;
        _messageHandler = messageHandler;
        _myChatMemberHandler = myChatMemberHandler;
    }

    public void Handle(Update update)
    {
        _logger.LogDebug("Update of {UpdateType} type is received", update.Type);
        switch (update.Type)
        {
            case UpdateType.Message:
                _messageHandler.Handle(update);
                break;
            case UpdateType.MyChatMember:
                _myChatMemberHandler.Handle(update);
                break;
            default:
                _logger.LogWarning("No handler for update of {UpdateType} type is found", update.Type);
                return;
        }
        _logger.LogDebug("Update of {UpdateType} type is handled", update.Type);
    }
}
