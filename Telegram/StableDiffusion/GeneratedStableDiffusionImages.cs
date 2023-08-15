using GIBS.CallbackQueries.Serialization;
using GIBS.Media;
using System.Runtime.CompilerServices;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.MediaFiles.Images;

namespace Telegram.StableDiffusion;

public class GeneratedStableDiffusionImages
{
    readonly MediaFilesCache _mediaFilesCache;
    readonly CallbackQuerySerializer _callbackQuerySerializer;
    readonly TelegramBot _bot;

    public GeneratedStableDiffusionImages(
        MediaFilesCache mediaFilesCache,
        CallbackQuerySerializer callbackQuerySerializer,
        TelegramBot bot)
    {
        _mediaFilesCache = mediaFilesCache;
        _callbackQuerySerializer = callbackQuerySerializer;
        _bot = bot;
    }

    internal async IAsyncEnumerable<MediaFilesCache.Entry> DownloadAsyncFrom(
        IFormFileCollection generatedImages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var generatedImage in generatedImages.ToAsyncEnumerable())
            yield return await _mediaFilesCache.AddAsync(generatedImage, cancellationToken);
    }

    /// <param name="cachedGeneratedImages">
    /// Has to be <see cref="IList{T}"/> and not <see cref="IEnumerable{T}"/> because
    /// <see cref="IEnumerable{T}"/> keeps <see cref="FileStream"/>s open and prevents them from closing.
    /// </param>
    internal async Task<Message[]> SendAsync(
        ChatId chatId,
        int replyToMessageId,
        Guid promptId,
        IEnumerable<MediaFilesCache.Entry> cachedGeneratedImages,
        CancellationToken cancellationToken)
    {
        var sentImages = await SendImagesAsync();
        try { return sentImages; }
        finally { await SendButtonsAsync(); }


        async Task<Message[]> SendImagesAsync()
        {
            var openCachedGeneratedImages = cachedGeneratedImages.Select(image => image.File.OpenRead()).ToArray();
            try
            {
                return await _bot.SendAlbumAsync_(chatId,
                    openCachedGeneratedImages.Select(
                        image => new InputMediaPhoto(
                            new InputMedia(image, Path.GetFileName(image.Name))))
                        .ToArray(),
                    replyToMessageId: replyToMessageId,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                foreach (var openCachedGeneratedImage in openCachedGeneratedImages)
                    await openCachedGeneratedImage.DisposeAsync();
            }
        }

        async Task<Message> SendButtonsAsync()
        {
            const string Upscale = "U";
            const string Vectorize = "V";
            const string Regenerate = "↻";

            return await _bot.SendMessageAsync_(chatId, ButtonsDescription(),
                new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                {
                    cachedGeneratedImages.Select((image, position)
                        => InlineKeyboardButton.WithCallbackData($"{Upscale}{position + 1}",
                        _callbackQuerySerializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                        .Data(ImageProcessingCallbackData.UpscaleImage | ImageProcessingCallbackData.UploadImage)
                        .Arguments(image.Index)
                        .Build()))).ToArray(),

                    cachedGeneratedImages.Select((image, position)
                        => InlineKeyboardButton.WithCallbackData($"{Vectorize}{position + 1}",
                        _callbackQuerySerializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                        .Data(ImageProcessingCallbackData.VectorizeImage | ImageProcessingCallbackData.UploadImage)
                        .Arguments(image.Index)
                        .Build()))).ToArray(),

                    new InlineKeyboardButton[]
                    {
                        InlineKeyboardButton.WithCallbackData($"{Regenerate}",
                        _callbackQuerySerializer.Serialize(new StableDiffusionCallbackQuery.Builder<StableDiffusionCallbackQuery>()
                        .Data(StableDiffusionCallbackData.Regenerate)
                        .Arguments(promptId)
                        .Build()))
                    }
                }),
                replyToMessageId: sentImages.First().MessageId,
                cancellationToken: cancellationToken);


            string ButtonsDescription()
                => $"{Upscale} - {nameof(Upscale)} | {Vectorize} - {nameof(Vectorize)} | {Regenerate} - {nameof(Regenerate)}";
        }
    }
}
