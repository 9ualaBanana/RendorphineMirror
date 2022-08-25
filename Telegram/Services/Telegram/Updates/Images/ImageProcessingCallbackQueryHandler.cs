using Common.Tasks.Model;
using Common.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Common;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Services.Telegram.FileRegistry;
using Telegram.Services.Tasks;
using Telegram.Services.Telegram.Authentication;
using Telegram.Services.Telegram.Updates.Tasks;

namespace Telegram.Services.Telegram.Updates.Images;

public class ImageProcessingCallbackQueryHandler : AuthenticatedTelegramUpdateHandler
{
    readonly TaskRegistry _taskRegistry;
    readonly TelegramFileRegistry _fileRegistry;
    readonly string _hostUrl;



    public ImageProcessingCallbackQueryHandler(
        ILogger<ImageProcessingCallbackQueryHandler> logger,
        TelegramBot bot,
        TelegramChatIdAuthenticator authenticator,
        TaskRegistry taskRegistry,
        TelegramFileRegistry fileRegistry,
        IConfiguration configuration) : base(logger, bot, authenticator)
    {
        _taskRegistry = taskRegistry;
        _fileRegistry = fileRegistry;
        _hostUrl = configuration["Host"];
    }



    protected async override Task HandleAsync(Update update, TelegramAuthenticationToken authenticationToken)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;

        var imageCallbackData = new ImageProcessingCallbackData(update.CallbackQuery.Data!);

        var fileId = _fileRegistry.TryGet(imageCallbackData.FileRegistryKey);
        if (fileId is null)
        { await Bot.TrySendMessageAsync(chatId, "Image is expired. Try to send it again."); return; }

        if (imageCallbackData.Value.HasFlag(ImageProcessingQueryFlags.Upscale) && imageCallbackData.Value.HasFlag(ImageProcessingQueryFlags.Upload))
        {
            var imagePath = Path.Combine(_fileRegistry.Path, imageCallbackData.FileRegistryKey);
            using (var downloadedImage = System.IO.File.Create(imagePath))
                await Bot.GetInfoAndDownloadFileAsync(fileId, downloadedImage);

            var taskId = (await TaskRegistration.RegisterAsync(
                new TaskCreationInfo(
                    PluginType.Python_Esrgan,
                    "EsrganUpscale",
                    null,
                    new DownloadLinkTaskInputInfo($"{_hostUrl}/tasks/getinput/{imageCallbackData.FileRegistryKey}"),
                    new MPlusTaskOutputInfo($"{imageCallbackData.FileRegistryKey}.jpg", "upscaled"), new()
                    ), authenticationToken.SessionId)).Result;
            _taskRegistry[taskId] = authenticationToken;

            await Bot.TrySendMessageAsync(chatId, "Resulting image will be sent back to you as soon as it's ready.",
                new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Progress", TaskCallbackData.Serialize(TaskQueryFlags.Details, taskId)))
                );
        }
    }
}
