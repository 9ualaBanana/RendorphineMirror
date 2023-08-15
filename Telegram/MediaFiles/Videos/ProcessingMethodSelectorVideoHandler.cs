using GIBS.CallbackQueries.Serialization;
using GIBS.Media;
using GIBS.Media.Videos;
using GIBS.MediaFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Localization.Resources;

namespace Telegram.MediaFiles.Videos;

public class ProcessingMethodSelectorVideoHandler : VideoHandler_
{
    readonly LocalizedText.Media _localizedMediaText;

    public ProcessingMethodSelectorVideoHandler(
        MediaFilesCache cache,
        MediaFile.Factory factory,
        CallbackQuerySerializer serializer,
        LocalizedText.Media localizedMediaText,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessingMethodSelectorVideoHandler> logger)
        : base(cache, factory, serializer, bot, httpContextAccessor, logger)
    {
        _localizedMediaText = localizedMediaText;
    }

    public override async Task HandleAsync()
    {
        var receivedVideo = await MediaFile.CreateAsyncFrom(Message, RequestAborted);
        if (!Enum.TryParse<Extension>(receivedVideo.Extension.TrimStart('.'), ignoreCase: true, out var _))
        { await Bot.SendMessageAsync_(ChatId, $"Videos with {receivedVideo.Extension} extension are not supported for processing."); return; }

        await Bot.SendMessageAsync_(ChatId, $"*{_localizedMediaText.ChooseHowToProcess}*",
            await BuildReplyMarkupAsyncFor(receivedVideo, RequestAborted)
            );
    }

    async Task<InlineKeyboardMarkup> BuildReplyMarkupAsyncFor(MediaFile receivedVideo, CancellationToken cancellationToken)
    {
        var cachedVideo = await Cache.AddAsync(receivedVideo, cancellationToken);
        return new(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(_localizedMediaText.UploadButton,
                CallbackQuery.Serialize(new VideoProcessingCallbackQuery.Builder<VideoProcessingCallbackQuery>()
                .Data(VideoProcessingCallbackData.UploadVideo)
                .Arguments(cachedVideo.Index)
                .Build()))
            }
        });
    }
}
