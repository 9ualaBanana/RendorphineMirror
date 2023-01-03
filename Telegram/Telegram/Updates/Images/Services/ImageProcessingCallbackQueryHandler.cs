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
        else if (mediaFileProcessingCallbackData.Value.HasFlag(ImageProcessingQueryFlags.VectorizeImage) && mediaFileProcessingCallbackData.Value.HasFlag(ImageProcessingQueryFlags.UploadImage))
            await VectorizeAndUploadToMPlusAsync(ChatIdFrom(update), (mediaFileProcessingCallbackData as ImageProcessingCallbackData)!);
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

    async Task VectorizeAndUploadToMPlusAsync(ChatId chatId, ImageProcessingCallbackData imageCallbackData)
    {
        await Bot.TrySendMessageAsync(chatId, "Please, choose a polygonality of the resulting image.",
            new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("500", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 500)),
                    InlineKeyboardButton.WithCallbackData("750", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 750))
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("1000", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 1000)),
                    InlineKeyboardButton.WithCallbackData("1250", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 1250))
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("1500", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 1500)),
                    InlineKeyboardButton.WithCallbackData("1750", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 1750))
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("2000", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 2000)),
                    InlineKeyboardButton.WithCallbackData("2250", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 2250))
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("2500", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 2500)),
                    InlineKeyboardButton.WithCallbackData("3000", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 3000))
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("5000", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 5000)),
                    InlineKeyboardButton.WithCallbackData("7000", VectorizerCallbackData.Serialize(VectorizerQueryFlags.Vectorize, imageCallbackData.FileRegistryKey, 7000))
                }
            }));
    }
}
