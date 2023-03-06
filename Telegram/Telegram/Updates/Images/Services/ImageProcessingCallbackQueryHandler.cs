using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Images.Models;
using Telegram.Telegram.FileRegistry;
using Telegram.Bot;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Options;
using Telegram.Tasks;
using Telegram.Tasks.CallbackQuery;
using Telegram.Models;

namespace Telegram.Telegram.Updates.Images.Services;

public class ImageProcessingCallbackQueryHandler : MediaFileProcessingCallbackQueryHandler
{
    readonly RegisteredTasksCache _registeredTasksCache;
    readonly string _hostUrl;


    public ImageProcessingCallbackQueryHandler(
        ILogger<ImageProcessingCallbackQueryHandler> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        RegisteredTasksCache registeredTasksCache,
        CachedMediaFiles cachedFiles,
        IOptions<TelegramBotOptions> botOptions,
        MediaFileDownloader telegramMediaFilesDownloader,
        IHttpClientFactory httpClientFactory) : base(logger, bot, authenticator, cachedFiles, telegramMediaFilesDownloader, httpClientFactory)
    {
        _registeredTasksCache = registeredTasksCache;
        _hostUrl = botOptions.Value.Host.ToString();
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
        else if (mediaFileProcessingCallbackData.Value.HasFlag(ImageProcessingQueryFlags.VectorizeImage) && mediaFileProcessingCallbackData.Value.HasFlag(ImageProcessingQueryFlags.UploadImage))
            await VectorizeAndUploadToMPlusAsync(ChatIdFrom(update), (mediaFileProcessingCallbackData as ImageProcessingCallbackData)!, authenticationToken);
        else if (mediaFileProcessingCallbackData.Value.HasFlag(ImageProcessingQueryFlags.UploadImage))  // Must be last conditional as it's the least specific.
            await UploadToMPlusAsync(ChatIdFrom(update), mediaFilePath, authenticationToken);
    }


    async Task UpscaleAndUploadToMPlusAsync(ChatId chatId, ImageProcessingCallbackData imageCallbackData, ChatAuthenticationToken authenticationToken)
    {
        var taskId = (await TaskRegistration.RegisterAsync(
            new TaskCreationInfo(
                PluginType.Python_Esrgan,
                "EsrganUpscale",
                null,
                new DownloadLinkTaskInputInfo($"{_hostUrl}/tasks/getinput/{imageCallbackData.FileCacheKey}"),
                new MPlusTaskOutputInfo($"{imageCallbackData.FileCacheKey}.jpg", "upscaled"),
                new()),
            authenticationToken.MPlus.SessionId))
            .Result;
        _registeredTasksCache[taskId] = authenticationToken;

        await Bot.SendMessageAsync_(chatId, "Resulting media file will be sent back to you as soon as it's ready.",
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Progress", TaskCallbackData.Serialize(TaskCallbackQueryFlags.Details, taskId)))
            );
    }

    async Task VectorizeAndUploadToMPlusAsync(ChatId chatId, ImageProcessingCallbackData imageCallbackData, ChatAuthenticationToken authenticationToken)
    {
        var taskId = (await TaskRegistration.RegisterAsync(
            new TaskCreationInfo(
                PluginType.VeeeVectorizer,
                "VeeeVectorize",
                default,
                new DownloadLinkTaskInputInfo($"{_hostUrl}/tasks/getinput/{imageCallbackData.FileCacheKey}"),
                new MPlusTaskOutputInfo($"{imageCallbackData.FileCacheKey}", "vectorized", 3_600_000),
                JObject.FromObject(new VeeeVectorizeInfo() { Lods = new int[] { 500, 2000, 4000, 7000, 8500, 10000 } })),
            authenticationToken.MPlus.SessionId))
            .Result;
        _registeredTasksCache[taskId] = authenticationToken;

        await Bot.SendMessageAsync_(chatId, "Resulting media files will be sent back to you as soon as they are ready.",
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Progress", TaskCallbackData.Serialize(TaskCallbackQueryFlags.Details, taskId)))
            );
    }
}
