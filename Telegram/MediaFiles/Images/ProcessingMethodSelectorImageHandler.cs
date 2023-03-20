using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Infrastructure;
using Telegram.Infrastructure.CallbackQueries;
using Telegram.Infrastructure.MediaFiles;
using Telegram.MPlus;

namespace Telegram.MediaFiles.Images;

public class ProcessingMethodSelectorImageHandler : UpdateHandler
{
    readonly MPlusTaskLauncherClient _taskLauncherClient;
    readonly MediaFilesCache _mediaFilesCache;
    readonly CallbackQuerySerializer _serializer;

    public ProcessingMethodSelectorImageHandler(
        MPlusTaskLauncherClient taskLauncherClient,
        MediaFilesCache mediaFilesCache,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessingMethodSelectorImageHandler> logger)
        : base(bot, httpContextAccessor, logger)
    {
        _taskLauncherClient = taskLauncherClient;
        _mediaFilesCache = mediaFilesCache;
        _serializer = serializer;
    }

    public override async Task HandleAsync(HttpContext context)
    {
        var message = Update.Message!;

        try
        {
            var receivedImage = MediaFile.From(message);
            await Bot.SendMessageAsync_(
                message.Chat.Id,
                "*Choose how to process the image*",
                await BuildReplyMarkupAsyncFor(receivedImage, context.RequestAborted));
        }
        catch (ArgumentException ex) when (ex.ParamName is not null)
        {
            await Bot.SendMessageAsync_(
                message.Chat.Id,
                $"{ex.Message}\n" +
                $"Specify an extension as the caption of the document.",
                cancellationToken: context.RequestAborted);
        }
    }

    async Task<InlineKeyboardMarkup> BuildReplyMarkupAsyncFor(MediaFile receivedImage, CancellationToken cancellationToken)
    {
        var cachedImage = await _mediaFilesCache.CacheAsync(receivedImage, cancellationToken);
        var prices = await _taskLauncherClient.RequestTaskPricesAsync(cancellationToken);
        return new(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"Upload to M+ | Free",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"Upscale and upload to M+ | {prices[TaskAction.EsrganUpscale]}€",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.UpscaleImage | ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData($"Vectorize and upload to M+ | {prices[TaskAction.VeeeVectorize]}€",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.VectorizeImage | ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            }
        });
    }
}
