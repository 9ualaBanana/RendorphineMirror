using GIBS.CallbackQueries.Serialization;
using GIBS.Media;
using Telegram.Localization.Resources;

namespace Telegram.MediaFiles.Videos;

public class VideoProcessingCallbackQueryHandler
    : MediaProcessingCallbackQueryHandler<VideoProcessingCallbackQuery, VideoProcessingCallbackData>
{
    public VideoProcessingCallbackQueryHandler(
        MediaFilesCache mediaFilesCache,
        LocalizedText.Media localizedMediaText,
        IHttpClientFactory httpClientFactory,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<VideoProcessingCallbackQueryHandler> logger)
        : base(localizedMediaText, mediaFilesCache, httpClientFactory, serializer, bot, httpContextAccessor, logger)
    {
    }

    protected override async Task HandleAsync(VideoProcessingCallbackQuery callbackQuery, MediaFilesCache.Entry cachedVideo)
        => await (callbackQuery.Data switch
        {
            VideoProcessingCallbackData.UploadVideo
                => UploadToMPlusAsync(cachedVideo),
            _ => HandleUnknownCallbackData()
        });
}

public record VideoProcessingCallbackQuery : MediaProcessingCallbackQuery<VideoProcessingCallbackData>
{
}

[Flags]
public enum VideoProcessingCallbackData
{
    UploadVideo
}
