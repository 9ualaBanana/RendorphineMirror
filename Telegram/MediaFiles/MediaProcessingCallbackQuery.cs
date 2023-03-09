using Telegram.Bot;
using Telegram.CallbackQueries;
using Telegram.MPlus;
using Transport.Upload;

namespace Telegram.MediaFiles;

public abstract class MediaProcessingCallbackQueryHandler<TCallbackQuery, ECallbackData>
    : CallbackQueryHandler<TCallbackQuery, ECallbackData>
    where TCallbackQuery : MediaProcessingCallbackQuery<ECallbackData>, new()
    where ECallbackData : struct, Enum
{
    readonly MediaFilesManager _mediaFilesManager;
    readonly HttpClient _httpClient;

    protected MediaProcessingCallbackQueryHandler(
        MediaFilesManager mediaFilesManager,
        IHttpClientFactory httpClientFactory,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
        : base(serializer, bot, httpContextAccessor, logger)
    {
        _mediaFilesManager = mediaFilesManager;
        _httpClient = httpClientFactory.CreateClient();
    }

    public override async Task HandleAsync(TCallbackQuery callbackQuery, HttpContext context)
    {
        if (_mediaFilesManager.Cache.RetrieveMediaFileWith(callbackQuery.CachedMediaFileIndex) is CachedMediaFile cachedMediaFile)
            await HandleAsync(callbackQuery, cachedMediaFile, context);
        else await Bot.SendMessageAsync_(ChatId, "Media file is expired. Try to send it again.");
    }

    protected abstract Task HandleAsync(TCallbackQuery callbackQuery, CachedMediaFile cachedMediaFile, HttpContext context);


    protected async Task UploadToMPlusAsync(CachedMediaFile cachedMediaFile, HttpContext context)
    {
        await Bot.SendMessageAsync_(ChatId, "Uploading the media file to M+...");

        // TODO: Extract PacketsTransporter to a separate service to get rid of the direct HttpClient dependency here.
        try { await PacketsTransporter.UploadAsync(new MPlusUploadSessionData(cachedMediaFile.Path, MPlusIdentity.SessionIdOf(context.User)), _httpClient); }
        catch { await Bot.SendMessageAsync_(ChatId, "Upload failed due to an unexpected error."); return; }

        await Bot.SendMessageAsync_(ChatId, "The media file was succesfully uploaded to M+.");
    }
}

public abstract record MediaProcessingCallbackQuery<ECallbackData> : CallbackQuery<ECallbackData>
    where ECallbackData : struct, Enum
{
    internal string CachedMediaFileIndex => ArgumentAt(0).ToString()!;
}
