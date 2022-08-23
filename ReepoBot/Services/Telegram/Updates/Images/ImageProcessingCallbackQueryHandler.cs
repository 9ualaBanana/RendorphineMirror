using Common.Tasks.Model;
using Common.Tasks;
using Telegram.Bot.Types;
using ReepoBot.Services.Telegram.Authentication;
using Telegram.Bot;
using ReepoBot.Services.Telegram.FileRegistry;
using Common;
using Telegram.Bot.Types.ReplyMarkups;
using ReepoBot.Services.Telegram.Updates.Tasks;
using ReepoBot.Services.Tasks;

namespace ReepoBot.Services.Telegram.Updates.Images;

public class ImageProcessingCallbackQueryHandler
{
    readonly ILogger _logger;
    readonly TelegramBot _bot;
    readonly TelegramChatIdAuthentication _authentication;
    readonly TaskRegistry _taskRegistry;
    readonly TelegramFileRegistry _fileRegistry;
    readonly string _hostUrl;

    public ImageProcessingCallbackQueryHandler(
        ILogger<ImageProcessingCallbackQueryHandler> logger,
        TelegramBot bot,
        TelegramChatIdAuthentication authentication,
        TaskRegistry taskRegistry,
        TelegramFileRegistry fileRegistry,
        IConfiguration configuration)
    {
        _logger = logger;
        _bot = bot;
        _authentication = authentication;
        _taskRegistry = taskRegistry;
        _fileRegistry = fileRegistry;
        _hostUrl = configuration["Host"];
    }

    public async Task HandleAsync(Update update)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;

        var authenticationToken = _authentication.GetTokenFor(chatId);
        if (authenticationToken is null) return;

        var imageCallbackData = new ImageProcessingCallbackData(update.CallbackQuery.Data!);
        
        var fileId = _fileRegistry.TryGet(imageCallbackData.FileRegistryKey);
        if (fileId is null)
        { await _bot.TrySendMessageAsync(chatId, "Image is expired. Try to send it again."); return; }

        if (imageCallbackData.Value.HasFlag(ImageProcessingQueryFlags.Upscale) && imageCallbackData.Value.HasFlag(ImageProcessingQueryFlags.Upload))
        {
            var imagePath = Path.Combine(_fileRegistry.Path, imageCallbackData.FileRegistryKey);
            using (var downloadedImage = System.IO.File.Create(imagePath))
                await _bot.GetInfoAndDownloadFileAsync(fileId, downloadedImage);

            var taskId = (await TaskRegistration.RegisterAsync(
                new TaskCreationInfo(
                    PluginType.Python_Esrgan,
                    "EsrganUpscale",
                    null,
                    new DownloadLinkTaskInputInfo($"{_hostUrl}/tasks/getinput/{imageCallbackData.FileRegistryKey}"),
                    new MPlusTaskOutputInfo($"{imageCallbackData.FileRegistryKey}.jpg", "upscaled"), new()
                    ), authenticationToken.SessionId)).Result;
            _taskRegistry[taskId] = authenticationToken.SessionId;

            await _bot.TrySendMessageAsync(chatId, "Resulting image will be sent back to you as soon as it's ready.",
                new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Progress", TaskCallbackData.Serialize(TaskQueryFlags.Details, taskId)))
                );
        }
    }
}
