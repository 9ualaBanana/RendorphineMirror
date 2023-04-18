﻿using Telegram.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.MediaFiles;

namespace Telegram.MediaFiles.Videos;

public class VideoProcessingCallbackQueryHandler
    : MediaProcessingCallbackQueryHandler<VideoProcessingCallbackQuery, VideoProcessingCallbackData>
{
    public VideoProcessingCallbackQueryHandler(
        MediaFilesManager mediaFilesManager,
        IHttpClientFactory httpClientFactory,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<VideoProcessingCallbackQueryHandler> logger)
        : base(mediaFilesManager, httpClientFactory, serializer, bot, httpContextAccessor, logger)
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
