using Common.Tasks.Model;
using Common.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Services.Telegram.FileRegistry;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Images.Models;
using Telegram.Telegram.Updates.Tasks.Models;
using Telegram.Telegram.Updates.Tasks.Services;
using Common.Plugins;
using Common.Tasks.Info;
using Newtonsoft.Json.Linq;

namespace Telegram.Telegram.Updates.Images.Services;

public class ImageProcessingCallbackQueryHandler : MediaFileProcessingCallbackQueryHandler
{
    readonly TaskRegistry _taskRegistry;
    readonly string _hostUrl;


    public ImageProcessingCallbackQueryHandler(
        ILogger<ImageProcessingCallbackQueryHandler> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        TaskRegistry taskRegistry,
        TelegramFileRegistry fileRegistry,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory) : base(logger, bot, authenticator, fileRegistry, httpClientFactory)
    {
        _taskRegistry = taskRegistry;
        _hostUrl = configuration["Host"];
    }

    protected async override Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken) =>
        await HandleAsync(update, authenticationToken, new ImageProcessingCallbackData(CallbackDataFrom(update)));

    protected override async Task Process<T>(
        Update update,
        ChatAuthenticationToken authenticationToken,
        MediaFileProcessingCallbackData<T> mediaFileProcessingCallbackData,
        string mediaFilePath)
    {
        if (mediaFileProcessingCallbackData.Value.HasFlag(ImageProcessingQueryFlags.UpscaleImage) && mediaFileProcessingCallbackData.Value.HasFlag(ImageProcessingQueryFlags.UploadImage))
            await UpscaleAndUploadToMPlusAsync(ChatIdFrom(update), (mediaFileProcessingCallbackData as ImageProcessingCallbackData)!, authenticationToken);
        else if (mediaFileProcessingCallbackData.Value.HasFlag(ImageProcessingQueryFlags.UploadImage))
            await UploadToMPlusAsync(ChatIdFrom(update), mediaFilePath, authenticationToken);
        else if (mediaFileProcessingCallbackData.Value.HasFlag(ImageProcessingQueryFlags.VectorizeImage) && mediaFileProcessingCallbackData.Value.HasFlag(ImageProcessingQueryFlags.UploadImage))
            await VectorizeAdnUploadToMPlusAsync(ChatIdFrom(update), (mediaFileProcessingCallbackData as ImageProcessingCallbackData)!, authenticationToken);
    }


    async Task UpscaleAndUploadToMPlusAsync(ChatId chatId, ImageProcessingCallbackData imageCallbackData, ChatAuthenticationToken authenticationToken)
    {
        var taskId = (await TaskRegistration.RegisterAsync(
            new TaskCreationInfo(
                PluginType.Python_Esrgan,
                "EsrganUpscale",
                null,
                new DownloadLinkTaskInputInfo($"{_hostUrl}/tasks/getinput/{imageCallbackData.FileRegistryKey}"),
                new MPlusTaskOutputInfo($"{imageCallbackData.FileRegistryKey}.jpg", "upscaled"),
                new()),
            authenticationToken.MPlus.SessionId))
            .Result;
        _taskRegistry[taskId] = authenticationToken;

        await Bot.TrySendMessageAsync(chatId, "Resulting media file will be sent back to you as soon as it's ready.",
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Progress", TaskCallbackData.Serialize(TaskQueryFlags.Details, taskId)))
            );
    }

    async Task VectorizeAdnUploadToMPlusAsync(ChatId chatId, ImageProcessingCallbackData imageCallbackData, ChatAuthenticationToken authenticationToken)
    {
        var taskId = (await TaskRegistration.RegisterAsync(
            new TaskCreationInfo(
                PluginType.VeeeVectorizer,
                "VeeeVectorize",
                default,
                new DownloadLinkTaskInputInfo($"{_hostUrl}/tasks/getinput/{imageCallbackData.FileRegistryKey}"),
                new MPlusTaskOutputInfo($"{imageCallbackData.FileRegistryKey}", "vectorized"),
                JObject.FromObject(new VeeeVectorizeInfo() { Lods = new int[] { 1750 } })),
            authenticationToken.MPlus.SessionId))
            .Result;
        _taskRegistry[taskId] = authenticationToken;

        await Bot.TrySendMessageAsync(chatId, "Resulting media file will be sent back to you as soon as it's ready.",
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Progress", TaskCallbackData.Serialize(TaskQueryFlags.Details, taskId)))
            );
    }
}
