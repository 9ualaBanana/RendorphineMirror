﻿using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.CallbackQueries;
using Telegram.Handlers;

namespace Telegram.MediaFiles.Images;

public class ProcessingMethodSelectorImageHandler : UpdateHandler
{
    readonly MediaFilesCache _mediaFilesCache;
    readonly CallbackQuerySerializer _serializer;

    public ProcessingMethodSelectorImageHandler(
        MediaFilesCache mediaFilesCache,
        CallbackQuerySerializer serializer,
        TelegramBot bot,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProcessingMethodSelectorImageHandler> logger)
        : base(bot, httpContextAccessor, logger)
    {
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
                replyMarkup: await BuildReplyMarkupAsyncFor(receivedImage, context.RequestAborted));
        }
        catch (ArgumentNullException ex)
        {
            await Bot.SendMessageAsync_(
                message.Chat.Id,
                $"{ex.Message}" +
                $"Specify an extension as the caption of the document.",
                cancellationToken: context.RequestAborted);
        }
    }

    async Task<InlineKeyboardMarkup> BuildReplyMarkupAsyncFor(MediaFile receivedImage, CancellationToken cancellationToken)
    {
        var cachedImage = await _mediaFilesCache.AddAsync(receivedImage, cancellationToken);
        return new(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("Upload to M+",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("Upscale and upload to M+",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.UpscaleImage | ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("Vectorize and upload to M+",
                _serializer.Serialize(new ImageProcessingCallbackQuery.Builder<ImageProcessingCallbackQuery>()
                .Data(ImageProcessingCallbackData.VectorizeImage | ImageProcessingCallbackData.UploadImage)
                .Arguments(cachedImage.Index)
                .Build()))
            }
        });
    }
}