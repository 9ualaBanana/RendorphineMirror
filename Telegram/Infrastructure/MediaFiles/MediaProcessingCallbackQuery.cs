using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Localization.Resources;
using Telegram.MPlus.Security;
using Transport.Upload;

namespace Telegram.Infrastructure.MediaFiles;

public abstract class MediaProcessingCallbackQueryHandler<TCallbackQuery, ECallbackData>
    : CallbackQueryHandler<TCallbackQuery, ECallbackData>
    where TCallbackQuery : MediaProcessingCallbackQuery<ECallbackData>, new()
    where ECallbackData : struct, Enum
{
    protected readonly LocalizedText.Media LocalizedMediaText;

    readonly MediaFilesCache _mediaFilesCache;
    readonly HttpClient _httpClient;

    protected MediaProcessingCallbackQueryHandler(
        LocalizedText.Media localizedMediaText,
        MediaFilesCache mediaFilesCache,
        IHttpClientFactory httpClientFactory,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
        : base(serializer, bot, httpContextAccessor, logger)
    {
        LocalizedMediaText = localizedMediaText;
        _mediaFilesCache = mediaFilesCache;
        _httpClient = httpClientFactory.CreateClient();
    }

    public override async Task HandleAsync(TCallbackQuery callbackQuery)
    {
        if (_mediaFilesCache.TryRetrieveMediaFileWith(callbackQuery.CachedMediaFileIndex) is MediaFilesCache.Entry cachedMediaFile)
            await HandleAsync(callbackQuery, cachedMediaFile);
        else await Bot.SendMessageAsync_(ChatId, LocalizedMediaText.Expired);
    }

    protected abstract Task HandleAsync(TCallbackQuery callbackQuery, MediaFilesCache.Entry cachedMediaFile);


    protected async Task UploadToMPlusAsync(MediaFilesCache.Entry cachedMediaFile)
    {
        await Bot.SendMessageAsync_(ChatId, LocalizedMediaText.Uploading);

        // TODO: Extract PacketsTransporter to a separate service to get rid of the direct HttpClient dependency here.
        try { await PacketsTransporter.UploadAsync(new MPlusUploadSessionData(cachedMediaFile.File.FullName, MPlusIdentity.SessionIdOf(User)), _httpClient); }
        catch { await Bot.SendMessageAsync_(ChatId, LocalizedMediaText.UploadFailed); return; }

        await Bot.SendMessageAsync_(ChatId, LocalizedMediaText.UploadSucceeded);
    }
}

public abstract record MediaProcessingCallbackQuery<ECallbackData> : CallbackQuery<ECallbackData>
    where ECallbackData : struct, Enum
{
    internal Guid CachedMediaFileIndex => Guid.Parse(ArgumentAt(0).ToString()!);
}
