using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.CallbackQueries;
using Telegram.Handlers;

namespace Telegram.MediaFiles.Videos;

public class ProcessingMethodSelectorVideoHandler : UpdateHandler
{
    readonly MediaFilesCache _mediaFilesCache;
    readonly CallbackQuerySerializer _callbackQuerySerializer;

    public ProcessingMethodSelectorVideoHandler(
        MediaFilesCache mediaFilesCache,
        CallbackQuerySerializer callbackQuerySerializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessingMethodSelectorVideoHandler> logger)
        : base(bot, httpContextAccessor, logger)
    {
        _mediaFilesCache = mediaFilesCache;
        _callbackQuerySerializer = callbackQuerySerializer;
    }

    public override async Task HandleAsync(HttpContext context)
        => await Bot.SendMessageAsync_(
            Update.Message!.Chat.Id,
            "*Choose how to process the video*",
            ReplyMarkupFor(MediaFile.From(Update.Message)));

    InlineKeyboardMarkup ReplyMarkupFor(MediaFile receivedVideo)
    {
        var cachedVideoIndex = _mediaFilesCache.Add(receivedVideo);
        return new(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("Upload to M+",
                _callbackQuerySerializer.Serialize(new VideoProcessingCallbackQuery.Builder<VideoProcessingCallbackQuery>()
                .Data(VideoProcessingCallbackData.UploadVideo)
                .Arguments(cachedVideoIndex)
                .Build()))
            }
        });
    }
}
