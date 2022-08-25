using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Services.Telegram.Updates.Images;
using Telegram.Services.Telegram.Updates.Tasks;

namespace Telegram.Services.Telegram.Updates;

public class TelegramCallbackQueryHandler
{
    readonly ILogger _logger;
    readonly TelegramBot _bot;
    readonly ImageProcessingCallbackQueryHandler _imageProcessingHandler;
    readonly TaskCallbackQueryHandler _taskHandler;

    public TelegramCallbackQueryHandler(
        ILogger<TelegramCallbackQueryHandler> logger,
        TelegramBot bot,
        ImageProcessingCallbackQueryHandler imageProcessingHandler,
        TaskCallbackQueryHandler taskHandler)
    {
        _logger = logger;
        _bot = bot;
        _imageProcessingHandler = imageProcessingHandler;
        _taskHandler = taskHandler;
    }

    public async Task HandleAsync(Update update)
    {
        await _bot.AnswerCallbackQueryAsync(update.CallbackQuery!.Id);
        _logger.LogDebug("Callback query with following data is received: {Data}", update.CallbackQuery!.Data);

        if (ImageProcessingCallbackData.Matches(update.CallbackQuery!.Data!))
        { await _imageProcessingHandler.HandleAsync(update); return; }
        if (TaskCallbackData.Matches(update.CallbackQuery!.Data!))
        { await _taskHandler.HandleAsync(update); return; }

        _logger.LogError("Callback query didn't match any handlers");
    }
}
