using Telegram.Bot.Types;
using Telegram.Services.Telegram.FileRegistry;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Transport.Upload;

namespace Telegram.Telegram.Updates.Images.Models;

public abstract class MediaFileProcessingCallbackQueryHandler : AuthenticatedTelegramUpdateHandler
{
    readonly TelegramFileRegistry _fileRegistry;
    readonly HttpClient _httpClient;


    protected MediaFileProcessingCallbackQueryHandler(ILogger logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        TelegramFileRegistry fileRegistry,
        IHttpClientFactory httpClientFactory) : base(logger, bot, authenticator)
    {
        _fileRegistry = fileRegistry;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task HandleAsync<T>(Update update, ChatAuthenticationToken authenticationToken, MediaFileProcessingCallbackData<T> mediaFileProcessingCallbackData) where T : struct, Enum
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;

        var mediaFile = _fileRegistry.TryGet(mediaFileProcessingCallbackData.FileRegistryKey);
        if (mediaFile is null)
        { await Bot.TrySendMessageAsync(chatId, "Media file is expired. Try to send it again."); return; }

        var mediaFilePath = Path.ChangeExtension(Path.Combine(_fileRegistry.Path, mediaFileProcessingCallbackData.FileRegistryKey), mediaFileProcessingCallbackData.ContentType.Extension);
        await mediaFile.Download(mediaFilePath, Bot);

        await Process(update, authenticationToken, mediaFileProcessingCallbackData, mediaFilePath);
    }

    protected abstract Task Process<T>(
        Update update,
        ChatAuthenticationToken authenticationToken,
        MediaFileProcessingCallbackData<T> mediaFileProcessingCallbackData,
        string mediaFilePath) where T : struct, Enum;


    protected async Task UploadToMPlusAsync(ChatId chatId, string imagePath, ChatAuthenticationToken authenticationToken)
    {
        await Bot.TrySendMessageAsync(chatId, "Uploading the media file to M+...");

        try { await PacketsTransporter.UploadAsync(new MPlusUploadSessionData(imagePath, authenticationToken.MPlus.SessionId), _httpClient); }
        catch { await Bot.TrySendMessageAsync(chatId, "Error occured trying to upload the media file to M+."); return; }

        await Bot.TrySendMessageAsync(chatId, "The media file was succesfully uploaded to M+.");
    }
}
