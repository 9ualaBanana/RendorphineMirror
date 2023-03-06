using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.MediaFiles;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Images.Models;
using Transport.Upload;

namespace Telegram.Telegram.Updates.Images.Services;

public abstract class MediaFileProcessingCallbackQueryHandler : AuthenticatedTelegramCallbackQueryHandlerBase
{
    readonly CachedMediaFiles _cachedFiles;
    readonly MediaFileDownloader _telegramMediaFilesDownloader;
    readonly HttpClient _httpClient;


    protected MediaFileProcessingCallbackQueryHandler(
        ILogger logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        CachedMediaFiles cachedFiles,
        MediaFileDownloader telegramMediaFilesDownloader,
        IHttpClientFactory httpClientFactory) : base(logger, bot, authenticator)
    {
        _cachedFiles = cachedFiles;
        _telegramMediaFilesDownloader = telegramMediaFilesDownloader;
        _httpClient = httpClientFactory.CreateClient();
    }


    protected async Task HandleAsync<T>(
        Update update,
        ChatAuthenticationToken authenticationToken,
        MediaFileProcessingCallbackData<T> mediaFileProcessingCallbackData) where T : struct, Enum
    {
        if (_cachedFiles[mediaFileProcessingCallbackData.FileCacheKey] is MediaFile telegramMediaFile)
        {
            var mediaFilePath = Path.ChangeExtension(
                Path.Combine(_cachedFiles.Location, mediaFileProcessingCallbackData.FileCacheKey),
                mediaFileProcessingCallbackData.ContentType.Extension);
            await _telegramMediaFilesDownloader.UseAsyncToDownload(telegramMediaFile, mediaFilePath, CancellationToken.None);

            await Process(update, authenticationToken, mediaFileProcessingCallbackData, mediaFilePath);
        }
        else { await Bot.SendMessageAsync_(ChatIdFrom(update), "Media file is expired. Try to send it again."); return; }
    }

    protected abstract Task Process<T>(
        Update update,
        ChatAuthenticationToken authenticationToken,
        MediaFileProcessingCallbackData<T> mediaFileProcessingCallbackData,
        string mediaFilePath) where T : struct, Enum;


    protected async Task UploadToMPlusAsync(ChatId chatId, string imagePath, ChatAuthenticationToken authenticationToken)
    {
        await Bot.SendMessageAsync_(chatId, "Uploading the media file to M+...");

        try { await PacketsTransporter.UploadAsync(new MPlusUploadSessionData(imagePath, authenticationToken.MPlus.SessionId), _httpClient); }
        catch { await Bot.SendMessageAsync_(chatId, "Error occured when trying to upload the media file to M+."); return; }

        await Bot.SendMessageAsync_(chatId, "The media file was succesfully uploaded to M+.");
    }
}
