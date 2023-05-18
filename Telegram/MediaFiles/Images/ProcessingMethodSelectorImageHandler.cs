using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure;
using Telegram.Infrastructure.CallbackQueries.Serialization;
using Telegram.Infrastructure.MediaFiles;
using Telegram.MPlus.Security;
using Telegram.Tasks;

namespace Telegram.MediaFiles.Images;

public class ProcessingMethodSelectorImageHandler : MessageHandler
{
    readonly TaskPrice _taskPrice;
    readonly MediaFilesCache _mediaFilesCache;
    readonly MediaFile.Factory _mediaFileFactory;
    readonly CallbackQuerySerializer _serializer;

    public ProcessingMethodSelectorImageHandler(
        TaskPrice taskPrice,
        MediaFilesCache mediaFilesCache,
        MediaFile.Factory mediaFileFactory,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessingMethodSelectorImageHandler> logger)
        : base(bot, httpContextAccessor, logger)
    {
        _taskPrice = taskPrice;
        _mediaFilesCache = mediaFilesCache;
        _mediaFileFactory = mediaFileFactory;
        _serializer = serializer;
    }

    public override async Task HandleAsync()
    {
        try
        {
            var receivedImage = await _mediaFileFactory.CreateAsyncFrom(Message, RequestAborted);
            var cachedImage = await _mediaFilesCache.AddAsync(receivedImage, RequestAborted);

            await Bot.SendMessageAsync_(ChatId, "*Choose how to process the image*",
                await BuildReplyMarkupAsyncFor(cachedImage)
                );
        }
        catch (ArgumentException ex) when (ex.ParamName is not null)
        {
            await Bot.SendMessageAsync_(ChatId,
                $"{ex.Message}\n" +
                $"Specify an extension as the caption of the document.",
                cancellationToken: RequestAborted);
        }
    }

    async Task<InlineKeyboardMarkup> BuildReplyMarkupAsyncFor(MediaFilesCache.Entry cachedImage)
    {
        return new(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"Upload to M+",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"Upscale and upload to M+{await PriceFor(TaskAction.EsrganUpscale)}",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.UpscaleImage | ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"Vectorize and upload to M+{await PriceFor(TaskAction.VeeeVectorize)}",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
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
