using Telegram.Bot.Types;
using Telegram.Services.Telegram.Updates.Commands;
using Telegram.Services.Telegram.Updates.Images;

namespace Telegram.Services.Telegram.Updates;

public class TelegramMessageHandler : TelegramUpdateHandler
{
    readonly TelegramCommandHandler _commandHandler;
    readonly TelegramImageHandler _imageHandler;



    public TelegramMessageHandler(
        ILogger<TelegramMessageHandler> logger,
        TelegramBot bot,
        TelegramCommandHandler commandHandler,
        TelegramImageHandler imageHandler) : base(logger, bot)
    {
        _commandHandler = commandHandler;
        _imageHandler = imageHandler;
    }



    public override async Task HandleAsync(Update update)
    {
        var message = update.Message!;
        Logger.LogDebug("Dispatching {Message}...", nameof(Message));

        if (IsCommand(message))
        { await _commandHandler.HandleAsync(update); return; }
        else if (IsImage(message))
        { await _imageHandler.HandleAsync(update); return; }
        else if (IsSystemMessage(message))
        {
            Logger.LogTrace("System messages are handled by {Handler}", nameof(TelegramChatMemberUpdatedHandler));
            return; // Bot adding and removal are handled via `UpdateType.MyChatMember` updates.
        }
    }

    static bool IsCommand(Message message) =>
        message.Text is not null && message.Text.StartsWith('/') && message.Text.Length > 1;

    static bool IsImage(Message message) => IsImage(message.Document) || message.Photo is not null;
    static bool IsImage(Document? document) =>
        document is not null && document.MimeType is not null && document.MimeType.StartsWith("image");

    bool IsSystemMessage(Message message)
    {
        return message.LeftChatMember?.Id == Bot.BotId || message.NewChatMembers?.First().Id == Bot.BotId;
    }
}