using Common.Tasks.Model;
using Common.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Services.Telegram.FileRegistry;
using Transport.Upload;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Images.Models;
using Telegram.Telegram.Updates.Tasks.Models;
using Telegram.Telegram.Updates.Tasks.Services;

namespace Telegram.Telegram.Updates.Images.Services;

public class ImageProcessingCallbackQueryHandler : AuthenticatedTelegramUpdateHandler
{
    readonly TaskRegistry _taskRegistry;
    readonly TelegramFileRegistry _fileRegistry;
    readonly string _hostUrl;
    readonly HttpClient _httpClient;


    public ImageProcessingCallbackQueryHandler(
        ILogger<ImageProcessingCallbackQueryHandler> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        TaskRegistry taskRegistry,
        TelegramFileRegistry fileRegistry,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory) : base(logger, bot, authenticator)
    {
        _taskRegistry = taskRegistry;
        _fileRegistry = fileRegistry;
        _hostUrl = configuration["Host"];
        _httpClient = httpClientFactory.CreateClient();
    }


    protected async override Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;

        var imageCallbackData = new ImageProcessingCallbackData(update.CallbackQuery.Data!);

        var inputOnlineFile = _fileRegistry.TryGet(imageCallbackData.FileRegistryKey);
        if (inputOnlineFile is null)
        { await Bot.TrySendMessageAsync(chatId, "Image is expired. Try to send it again."); return; }

        var imagePath = Path.ChangeExtension(Path.Combine(_fileRegistry.Path, imageCallbackData.FileRegistryKey), ".jpg");
        await TelegramImage.From(inputOnlineFile).Download(imagePath, Bot);

        if (imageCallbackData.Value.HasFlag(ImageProcessingQueryFlags.Upscale) && imageCallbackData.Value.HasFlag(ImageProcessingQueryFlags.Upload))
            await UpscaleAndUploadToMPlusAsync(chatId, imageCallbackData, authenticationToken);
        else if (imageCallbackData.Value.HasFlag(ImageProcessingQueryFlags.Upload))
            await UploadToMPlusAsync(chatId, imagePath, authenticationToken);
    }

    async Task UpscaleAndUploadToMPlusAsync(ChatId chatId, ImageProcessingCallbackData imageCallbackData, ChatAuthenticationToken authenticationToken)
    {
        var taskId = (await TaskRegistration.RegisterAsync(
            new TaskCreationInfo(
                PluginType.Python_Esrgan,
                "EsrganUpscale",
                null,
                new DownloadLinkTaskInputInfo($"{_hostUrl}/tasks/getinput/{imageCallbackData.FileRegistryKey}"),
                new MPlusTaskOutputInfo($"{imageCallbackData.FileRegistryKey}.jpg", "upscaled"), new()
                ), authenticationToken.MPlus.SessionId)).Result;
                _taskRegistry[taskId] = authenticationToken;

        await Bot.TrySendMessageAsync(chatId, "Resulting image will be sent back to you as soon as it's ready.",
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Progress", TaskCallbackData.Serialize(TaskQueryFlags.Details, taskId)))
            );
    }

    async Task UploadToMPlusAsync(ChatId chatId, string imagePath, ChatAuthenticationToken authenticationToken)
    {
        await Bot.TrySendMessageAsync(chatId, "Uploading the image to M+...");

        try { await PacketsTransporter.UploadAsync(new MPlusUploadSessionData(imagePath, authenticationToken.MPlus.SessionId), _httpClient); }
        catch { await Bot.TrySendMessageAsync(chatId, "Error occured trying to upload the image to M+."); return; }

        await Bot.TrySendMessageAsync(chatId, "The image was succesffully uploaded to M+.");
    }
}
