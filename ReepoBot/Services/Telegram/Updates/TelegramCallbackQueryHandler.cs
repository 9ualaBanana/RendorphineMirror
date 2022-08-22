using ReepoBot.Services.Telegram.Updates.Images;
using ReepoBot.Services.Telegram.Updates.Tasks;
using Telegram.Bot.Types;

namespace ReepoBot.Services.Telegram.Updates;

public class TelegramCallbackQueryHandler
{
    readonly ILogger _logger;
    readonly ImageProcessingCallbackQueryHandler _imageProcessingHandler;
    readonly TaskCallbackQueryHandler _taskHandler;

    public TelegramCallbackQueryHandler(
        ILogger<TelegramCallbackQueryHandler> logger,
        ImageProcessingCallbackQueryHandler imageProcessingHandler,
        TaskCallbackQueryHandler taskHandler)
    {
        _logger = logger;
        _imageProcessingHandler = imageProcessingHandler;
        _taskHandler = taskHandler;
    }

    public async Task HandleAsync(Update update)
    {
        _logger.LogDebug("Callback query with following data is received: {Data}", update.CallbackQuery!.Data);

        if (ImageProcessingCallbackData.Matches(update.CallbackQuery!.Data!))
        { await _imageProcessingHandler.HandleAsync(update); return; }
        if (TaskCallbackData.Matches(update.CallbackQuery!.Data!))
        { await _taskHandler.HandleAsync(update); return; }

        _logger.LogDebug("Callback query didn't match any handlers");
    }
}
