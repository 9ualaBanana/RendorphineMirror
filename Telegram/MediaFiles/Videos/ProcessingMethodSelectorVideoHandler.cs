using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Infrastructure.Messages;
using Telegram.Localization.Resources;

namespace Telegram.MediaFiles.Videos;

public class ProcessingMethodSelectorVideoHandler : MessageHandler_
{
    readonly MediaFilesCache _mediaFilesCache;
    readonly MediaFile.Factory _mediaFileFactory;
    readonly CallbackQuerySerializer _callbackQuerySerializer;
    readonly LocalizedText.Media _localizedMediaText;

    public ProcessingMethodSelectorVideoHandler(
        MediaFilesCache mediaFilesCache,
        MediaFile.Factory mediaFileFactory,
        CallbackQuerySerializer callbackQuerySerializer,
        LocalizedText.Media localizedMediaText,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessingMethodSelectorVideoHandler> logger)
        : base(bot, httpContextAccessor, logger)
    {
        _mediaFilesCache = mediaFilesCache;
        _mediaFileFactory = mediaFileFactory;
        _callbackQuerySerializer = callbackQuerySerializer;
        _localizedMediaText = localizedMediaText;
    }

    public override async Task HandleAsync()
        => await Bot.SendMessageAsync_(ChatId, $"*{_localizedMediaText.ChooseHowToProcess}*",
            await BuildReplyMarkupAsyncFor(await _mediaFileFactory.CreateAsyncFrom(Message, RequestAborted), RequestAborted)
            );

    async Task<InlineKeyboardMarkup> BuildReplyMarkupAsyncFor(MediaFile receivedVideo, CancellationToken cancellationToken)
    {
        var cachedVideo = await _mediaFilesCache.AddAsync(receivedVideo, cancellationToken);
        return new(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(_localizedMediaText.UploadButton,
                _callbackQuerySerializer.Serialize(new VideoProcessingCallbackQuery.Builder<VideoProcessingCallbackQuery>()
                .Data(VideoProcessingCallbackData.UploadVideo)
                .Arguments(cachedVideo.Index)
                .Build()))
            }
        });
    }
}
