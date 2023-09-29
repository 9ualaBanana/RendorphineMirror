using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure.Bot;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.MediaFiles;
using Telegram.Infrastructure.Messages;
using Telegram.Localization.Resources;
using Telegram.MPlus.Security;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

public class ProcessingMethodSelectorImageHandler : MessageHandler_
{
    readonly RTaskManager _rTaskManager;
    readonly MediaFilesCache _mediaFilesCache;
    readonly MediaFile.Factory _mediaFileFactory;
    readonly CallbackQuerySerializer _serializer;
    readonly LocalizedText.Media _localizedMediaText;

    public ProcessingMethodSelectorImageHandler(
        RTaskManager rTaskManager,
        MediaFilesCache mediaFilesCache,
        MediaFile.Factory mediaFileFactory,
        CallbackQuerySerializer serializer,
        LocalizedText.Media localizedMediaText,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessingMethodSelectorImageHandler> logger)
        : base(bot, httpContextAccessor, logger)
    {
        _rTaskManager = rTaskManager;
        _mediaFilesCache = mediaFilesCache;
        _mediaFileFactory = mediaFileFactory;
        _serializer = serializer;
        _localizedMediaText = localizedMediaText;
    }

    public override async Task HandleAsync()
    {
        try
        {
            var receivedImage = await _mediaFileFactory.CreateAsyncFrom(Message, RequestAborted);
            var cachedImage = await _mediaFilesCache.AddAsync(receivedImage, RequestAborted);

            await Bot.SendMessageAsync_(ChatId, $"*{_localizedMediaText.ChooseHowToProcess}*",
                await BuildReplyMarkupAsyncFor(cachedImage)
                );
        }
        catch (ArgumentException ex) when (ex.ParamName is not null)
        {
            await Bot.SendMessageAsync_(ChatId,
                $"{ex.Message}\n" +
                _localizedMediaText.SpecifyExtensionAsCaption,
                cancellationToken: RequestAborted);
        }
    }

    async Task<InlineKeyboardMarkup> BuildReplyMarkupAsyncFor(MediaFilesCache.Entry cachedImage)
    {
        return new(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(_localizedMediaText.UploadButton,
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"{_localizedMediaText.UpscaleButton}{await PriceFor(TaskAction.EsrganUpscale)}",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.UpscaleImage | ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"{_localizedMediaText.VectorizeButton}{await PriceFor(TaskAction.VeeeVectorize)}",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.VectorizeImage | ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            }
        });


        async Task<string?> PriceFor(TaskAction rTaskAction)
        {
            var price = await _rTaskManager.CalculatePriceAsyncFor(rTaskAction, MPlusIdentity.SessionIdOf(User), RequestAborted);
            return price is 0 ? null : $" | {price}€";
        }
    }
}
