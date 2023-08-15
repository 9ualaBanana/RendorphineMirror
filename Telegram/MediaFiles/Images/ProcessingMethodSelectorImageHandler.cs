using GIBS.CallbackQueries.Serialization;
using GIBS.Media;
using GIBS.Media.Images;
using GIBS.MediaFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Localization.Resources;
using Telegram.MPlus.Security;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

public class ProcessingMethodSelectorImageHandler : ImageHandler_
{
    readonly TaskPrice _taskPrice;
    readonly LocalizedText.Media _localizedMediaText;

    public ProcessingMethodSelectorImageHandler(
        TaskPrice taskPrice,
        MediaFilesCache cache,
        MediaFile.Factory factory,
        CallbackQuerySerializer serializer,
        LocalizedText.Media localizedMediaText,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessingMethodSelectorImageHandler> logger)
        : base(cache, factory, serializer, bot, httpContextAccessor, logger)
    {
        _taskPrice = taskPrice;
        _localizedMediaText = localizedMediaText;
    }

    public override async Task HandleAsync()
    {
        try
        {
            var receivedImage = await MediaFile.CreateAsyncFrom(Message, RequestAborted);
            if (!Enum.TryParse<Extension>(receivedImage.Extension.TrimStart('.'), ignoreCase: true, out var _))
            { await Bot.SendMessageAsync_(ChatId, $"Images with {receivedImage.Extension} extension are not supported for processing."); return; }
            
            var cachedImage = await Cache.AddAsync(receivedImage, RequestAborted);
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
                CallbackQuery.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"{_localizedMediaText.UpscaleButton}{await PriceFor(TaskAction.EsrganUpscale)}",
                CallbackQuery.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.UpscaleImage | ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"{_localizedMediaText.VectorizeButton}{await PriceFor(TaskAction.VeeeVectorize)}",
                CallbackQuery.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.VectorizeImage | ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            }
        });


        async Task<string?> PriceFor(TaskAction taskAction)
        {
            var price = await _taskPrice.CalculateConsideringBonusBalanceAsyncFor(taskAction, MPlusIdentity.SessionIdOf(User), RequestAborted);
            return price is 0 ? null : $" | {price}€";
        }
    }
}
