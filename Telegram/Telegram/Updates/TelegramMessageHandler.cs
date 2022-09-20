using Telegram.Bot.Types;
using Telegram.Services.Telegram.Updates.Commands;
using Telegram.Telegram.Updates.Images.Services;

namespace Telegram.Telegram.Updates;

public class TelegramMessageHandler : TelegramUpdateHandler
{
    readonly TelegramCommandHandler _commandHandler;
    readonly TelegramImageHandler _imageHandler;
    readonly TelegramVideoHandler _videoHandler;



    public TelegramMessageHandler(
        ILogger<TelegramMessageHandler> logger,
        TelegramBot bot,
        TelegramCommandHandler commandHandler,
        TelegramImageHandler imageHandler,
        TelegramVideoHandler videoHandler) : base(logger, bot)
    {
        _commandHandler = commandHandler;
        _imageHandler = imageHandler;
        _videoHandler = videoHandler;
    }



    public override async Task HandleAsync(Update update)
    {
        var message = update.Message!;
        Logger.LogDebug("Dispatching {Message}...", nameof(Message));

        if (IsCommand(message))
        { await _commandHandler.HandleAsync(update); return; }
        else if (IsVideo(message))  // Check for video must precede the one for image because Photo is not null for videos too.
        { await _videoHandler.HandleAsync(update); return; }
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

    static bool IsImage(Message message) =>
        IsImage(message.Document) || message.Photo is not null || Uri.IsWellFormedUriString(message.Text, UriKind.Absolute);
    static bool IsImage(Document? document) =>
        document is not null && document.MimeType is not null && document.MimeType.StartsWith("image");

    static bool IsVideo(Message message) => IsVideo(message.Document) || message.Video is not null;
    static bool IsVideo(Document? document) =>
        document is not null && document.MimeType is not null && document.MimeType.StartsWith("video");

    bool IsSystemMessage(Message message) =>
        message.LeftChatMember?.Id == Bot.BotId || message.NewChatMembers?.First().Id == Bot.BotId;
}