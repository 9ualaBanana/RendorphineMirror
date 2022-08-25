using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Services.Telegram.Updates.Images;
using Telegram.Services.Telegram.Updates.Tasks;

namespace Telegram.Services.Telegram.Updates;

public class TelegramCallbackQueryHandler : TelegramUpdateHandler
{
    readonly ImageProcessingCallbackQueryHandler _imageProcessingHandler;
    readonly TaskCallbackQueryHandler _taskHandler;



    public TelegramCallbackQueryHandler(
        ILogger<TelegramCallbackQueryHandler> logger,
        TelegramBot bot,
        ImageProcessingCallbackQueryHandler imageProcessingHandler,
        TaskCallbackQueryHandler taskHandler) : base(logger, bot)
    {
        _imageProcessingHandler = imageProcessingHandler;
        _taskHandler = taskHandler;
    }



    public override async Task HandleAsync(Update update)
    {
        var callbackQuery = update.CallbackQuery!;
        await AnswerCallbackQueryAsync(callbackQuery);

        if (ImageProcessingCallbackData.Matches(callbackQuery.Data!))
        { await _imageProcessingHandler.HandleAsync(update); return; }
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
