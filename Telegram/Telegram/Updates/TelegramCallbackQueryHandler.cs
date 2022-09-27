using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Telegram.MessageChunker.Models;
using Telegram.Telegram.MessageChunker.Services;
using Telegram.Telegram.Updates.Images.Models;
using Telegram.Telegram.Updates.Images.Services;
using Telegram.Telegram.Updates.Tasks.Models;
using Telegram.Telegram.Updates.Tasks.Services;

namespace Telegram.Telegram.Updates;

public class TelegramCallbackQueryHandler : TelegramUpdateHandler
{
    readonly MessageChunkerCallbackQueryHandler _messageChunkerHandler;
    readonly ImageProcessingCallbackQueryHandler _imageProcessingHandler;
    readonly VideoProcessingCallbackQueryHandler _videoProcessingHandler;
    readonly TaskCallbackQueryHandler _taskHandler;


    public TelegramCallbackQueryHandler(
        ILogger<TelegramCallbackQueryHandler> logger,
        TelegramBot bot,
        MessageChunkerCallbackQueryHandler messageChunkerHandler,
        ImageProcessingCallbackQueryHandler imageProcessingHandler,
        VideoProcessingCallbackQueryHandler videoProcessingHandler,
        TaskCallbackQueryHandler taskHandler) : base(logger, bot)
    {
        _messageChunkerHandler = messageChunkerHandler;
        _imageProcessingHandler = imageProcessingHandler;
        _videoProcessingHandler = videoProcessingHandler;
        _taskHandler = taskHandler;
    }


    // Refactor by enumearating over services of type TelegramCallbackData like in Commands.
    public override async Task HandleAsync(Update update)
    {
        var callbackQuery = update.CallbackQuery!;
        await AnswerCallbackQueryAsync(callbackQuery);

        if (MessageChunkerCallbackData.Matches(callbackQuery.Data!))
        { await _messageChunkerHandler.HandleAsync(update); return; }
        if (ImageProcessingCallbackData.Matches(callbackQuery.Data!))
        { await _imageProcessingHandler.HandleAsync(update); return; }
        if (VideoProcessingCallbackData.Matches(callbackQuery.Data!))
        { await _videoProcessingHandler.HandleAsync(update); return; }
        if (TaskCallbackData.Matches(callbackQuery.Data!))
        { await _taskHandler.HandleAsync(update); return; }

        Logger.LogError("Callback query didn't match any handlers");
    }

    async Task AnswerCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        await Bot.AnswerCallbackQueryAsync(callbackQuery.Id);
        Logger.LogDebug("Callback query with following data is received: {Data}", callbackQuery.Data);
    }
}
