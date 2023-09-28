using Microsoft.Extensions.Options;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Infrastructure.Tasks;
using Telegram.Localization.Resources;

namespace Telegram.MediaFiles.Images;

public class ImageProcessingCallbackQueryHandler
    : MediaProcessingCallbackQueryHandler<ImageProcessingCallbackQuery, ImageProcessingCallbackData>
{
    readonly BotRTask _botRTask;
    readonly Uri _hostUrl;

    public ImageProcessingCallbackQueryHandler(
        BotRTask botRTask,
        IOptions<TelegramBot.Options> botOptions,
        MediaFilesCache mediaFilesCache,
        LocalizedText.Media localizedMediaText,
        IHttpClientFactory httpClientFactory,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ImageProcessingCallbackQueryHandler> logger)
        : base(localizedMediaText, mediaFilesCache, httpClientFactory, serializer, bot, httpContextAccessor, logger)
    {
        _botRTask = botRTask;
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
        => await _botRTask.TryRegisterAsync(
            new TaskCreationInfo(
                TaskAction.EsrganUpscale,
                new DownloadLinkTaskInputInfo(new Uri(_hostUrl, $"tasks/getinput/{cachedImage.Index}")),
                new MPlusTaskOutputInfo(cachedImage.Index.ToString(), "upscaled") { CustomHost = _hostUrl.ToString() },
                TaskObject.From(cachedImage.File)),
            User.ToTelegramBotUserWith(ChatId)
            );

    async Task VectorizeAndUploadToMPlusAsync(MediaFilesCache.Entry cachedImage)
        => await _botRTask.TryRegisterAsync(
            new TaskCreationInfo(
                TaskAction.VeeeVectorize,
                new DownloadLinkTaskInputInfo(new Uri(_hostUrl, $"tasks/getinput/{cachedImage.Index}")),
                new MPlusTaskOutputInfo(cachedImage.Index.ToString(), "vectorized") { CustomHost = _hostUrl.ToString() },
                new VeeeVectorizeInfo(new int[] { 8500 }),
                TaskObject.From(cachedImage.File)),
            User.ToTelegramBotUserWith(ChatId)
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
