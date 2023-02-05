using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Images.Models;
using Telegram.Telegram.Updates.Tasks.Models;
using Telegram.Telegram.Updates.Tasks.Services;
using Telegram.Telegram.FileRegistry;
using Telegram.Bot;
using Common.Tasks.Info;
using Microsoft.AspNetCore.Authentication;
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

    async Task VectorizeAndUploadToMPlusAsync(ChatId chatId, ImageProcessingCallbackData imageCallbackData, ChatAuthenticationToken authenticationToken)
    {
        var taskId = (await TaskRegistration.RegisterAsync(
            new TaskCreationInfo(
                PluginType.VeeeVectorizer,
                "VeeeVectorize",
                default,
                new DownloadLinkTaskInputInfo($"{_hostUrl}/tasks/getinput/{imageCallbackData.FileRegistryKey}"),
                new MPlusTaskOutputInfo($"{imageCallbackData.FileRegistryKey}", "vectorized"),
                JObject.FromObject(new VeeeVectorizeInfo() { Lods = new int[] { 500, 2000, 4000, 7000, 8500, 10000 } })),
            authenticationToken.MPlus.SessionId))
            .Result;
        _taskRegistry[taskId] = authenticationToken;

        await Bot.TrySendMessageAsync(chatId, "Resulting media files will be sent back to you as soon as they are ready.",
            new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Progress", TaskCallbackData.Serialize(TaskQueryFlags.Details, taskId)))
            );
        //await Bot.TrySendMessageAsync(chatId, "Choose preferred level of details for the resulting image.",
        //    new InlineKeyboardMarkup(new InlineKeyboardButton[][]
        //    {
        //        new InlineKeyboardButton[]
        //        {
        //            InlineKeyboardButton.WithCallbackData("◭10000", VectorizerCallbackData.Serialize(VectorizerQueryFlags.V, imageCallbackData.FileRegistryKey, 10000)),
        //            InlineKeyboardButton.WithCallbackData("◮8500", VectorizerCallbackData.Serialize(VectorizerQueryFlags.V, imageCallbackData.FileRegistryKey, 8500))
        //        },
        //        new InlineKeyboardButton[]
        //        {
        //            InlineKeyboardButton.WithCallbackData("◭7000", VectorizerCallbackData.Serialize(VectorizerQueryFlags.V, imageCallbackData.FileRegistryKey, 7000)),
        //            InlineKeyboardButton.WithCallbackData("◮4000", VectorizerCallbackData.Serialize(VectorizerQueryFlags.V, imageCallbackData.FileRegistryKey, 4000))
        //        },
        //        new InlineKeyboardButton[]
        //        {
        //            InlineKeyboardButton.WithCallbackData("◭2000", VectorizerCallbackData.Serialize(VectorizerQueryFlags.V, imageCallbackData.FileRegistryKey, 2000)),
        //            InlineKeyboardButton.WithCallbackData("◮500", VectorizerCallbackData.Serialize(VectorizerQueryFlags.V, imageCallbackData.FileRegistryKey, 500))
        //        }
        //    }));
    }
}
