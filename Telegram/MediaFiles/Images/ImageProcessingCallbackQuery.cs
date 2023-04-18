using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Infrastructure.Tasks;
using Telegram.MPlus;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

public class ImageProcessingCallbackQueryHandler
    : MediaProcessingCallbackQueryHandler<ImageProcessingCallbackQuery, ImageProcessingCallbackData>
{
    readonly OwnedRegisteredTasksCache _ownedRegisteredTasksCache;
    readonly Uri _hostUrl;

    public ImageProcessingCallbackQueryHandler(
        OwnedRegisteredTasksCache ownedRegisteredTasksCache,
        IOptions<TelegramBotOptions> botOptions,
        MediaFilesManager mediaFilesManager,
        IHttpClientFactory httpClientFactory,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ImageProcessingCallbackQueryHandler> logger)
        : base(mediaFilesManager, httpClientFactory, serializer, bot, httpContextAccessor, logger)
    {
        _ownedRegisteredTasksCache = ownedRegisteredTasksCache;
        _hostUrl = botOptions.Value.Host;
    }

    protected override async Task HandleAsync(ImageProcessingCallbackQuery callbackQuery, MediaFilesCache.Entry cachedImage)
        => await (callbackQuery.Data switch
        {
            ImageProcessingCallbackData.UploadImage
                => UploadToMPlusAsync(cachedImage),
            ImageProcessingCallbackData.UpscaleImage | ImageProcessingCallbackData.UploadImage
                => UpscaleAndUploadToMPlusAsync(cachedImage),
            ImageProcessingCallbackData.VectorizeImage | ImageProcessingCallbackData.UploadImage
                => VectorizeAndUploadToMPlusAsync(cachedImage),
            _ => HandleUnknownCallbackData()
        });

    async Task UpscaleAndUploadToMPlusAsync(MediaFilesCache.Entry cachedImage)
    {
        var registeredTask = (await TaskRegistration.RegisterAsync(
            new TaskCreationInfo(
                TaskAction.EsrganUpscale,
                new DownloadLinkTaskInputInfo(new Uri(_hostUrl, $"tasks/getinput/{cachedImage.Index}")),
                new MPlusTaskOutputInfo(cachedImage.Index.ToString(), "upscaled"),
                TaskObject.From(cachedImage.File)),
            MPlusIdentity.SessionIdOf(User))).Result;

        _ownedRegisteredTasksCache.Add(registeredTask.OwnedBy(new TelegramBotUser(ChatId, User)));

        await Bot.SendMessageAsync_(ChatId, "Resulting image will be sent back to you as soon as it's ready.",
            new InlineKeyboardMarkup(DetailsButtonFor(registeredTask))
            );
    }

    async Task VectorizeAndUploadToMPlusAsync(MediaFilesCache.Entry cachedImage)
    {
        var registeredTask = (await TaskRegistration.RegisterAsync(
            new TaskCreationInfo(
                TaskAction.VeeeVectorize,
                new DownloadLinkTaskInputInfo(new Uri(_hostUrl, $"tasks/getinput/{cachedImage.Index}")),
                new MPlusTaskOutputInfo(cachedImage.Index.ToString(), "vectorized"),
                new VeeeVectorizeInfo() { Lods = new int[] { 8500 } },
                TaskObject.From(cachedImage.File)),
            MPlusIdentity.SessionIdOf(User))).Result;

        _ownedRegisteredTasksCache.Add(registeredTask.OwnedBy(new TelegramBotUser(ChatId, User)));

        await Bot.SendMessageAsync_(ChatId, "Resulting images will be sent back to you as soon as they are ready.",
            new InlineKeyboardMarkup(DetailsButtonFor(registeredTask))
            );
    }

    InlineKeyboardButton DetailsButtonFor(ITypedRegisteredTask typedRegisteredTask)
        => InlineKeyboardButton.WithCallbackData("Details",
            Serializer.Serialize(new TaskCallbackQuery.Builder<TaskCallbackQuery>()
                .Data(TaskCallbackData.Details)
                .Arguments(typedRegisteredTask.Id, typedRegisteredTask.Action)
                .Build())
            );
}

public record ImageProcessingCallbackQuery : MediaProcessingCallbackQuery<ImageProcessingCallbackData>
{
}

[Flags]
public enum ImageProcessingCallbackData
{
    UploadImage = 1,
    UpscaleImage = 2,
    VectorizeImage = 4,
}
