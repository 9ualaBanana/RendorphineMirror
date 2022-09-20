using Telegram.Bot.Types;
using Telegram.Services.Telegram.FileRegistry;
using Telegram.Telegram.Authentication.Models;
using Telegram.Telegram.Authentication.Services;
using Telegram.Telegram.Updates.Images.Models;

namespace Telegram.Telegram.Updates.Images.Services;

public class VideoProcessingCallbackQueryHandler : MediaFileProcessingCallbackQueryHandler
{

    public VideoProcessingCallbackQueryHandler(
        ILogger<VideoProcessingCallbackQueryHandler> logger,
        TelegramBot bot,
        ChatAuthenticator authenticator,
        TelegramFileRegistry fileRegistry,
        IHttpClientFactory httpClientFactory) : base(logger, bot, authenticator, fileRegistry, httpClientFactory)
    {
    }

    protected async override Task HandleAsync(Update update, ChatAuthenticationToken authenticationToken) =>
        await HandleAsync(update, authenticationToken, new VideoProcessingCallbackData(update.CallbackQuery!.Data!));

    protected override async Task Process<T>(
        Update update,
        ChatAuthenticationToken authenticationToken,
        MediaFileProcessingCallbackData<T> mediaFileProcessingCallbackData,
        string mediaFilePath)
    {
        var chatId = update.CallbackQuery!.Message!.Chat.Id;

        if (mediaFileProcessingCallbackData.Value.HasFlag(VideoProcessingQueryFlags.UploadVideo))
            await UploadToMPlusAsync(chatId, mediaFilePath, authenticationToken);
    }
}
