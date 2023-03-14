﻿using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram.FileRegistry;
using Telegram.Telegram.Updates.Images.Models;

namespace Telegram.Telegram.Updates.Images.Services;

public class TelegramImageHandler : TelegramUpdateHandler
{
    readonly CachedFiles _cachedFiles;


    public TelegramImageHandler(ILogger<TelegramImageHandler> logger, TelegramBot bot, CachedFiles cachedFiles)
        : base(logger, bot)
    {
        _cachedFiles = cachedFiles;
    }


    public override async Task HandleAsync(Update update)
    {
        await Bot.SendMessageAsync_(
            update.Message!.Chat.Id,
            "*Choose how to process the image*",
            replyMarkup: CreateReplyMarkupForLowResolutionImage(TelegramMediaFile.From(update.Message)));
    }

    InlineKeyboardMarkup ReplyMarkupFor(TelegramMediaFile mediaFile) => mediaFile.Size switch
    {
        < 20_000_000 => CreateReplyMarkupForLowResolutionImage(mediaFile),
        _ => CreateReplyMarkupForHighResolutionImage(mediaFile)
    };

    InlineKeyboardMarkup CreateReplyMarkupForLowResolutionImage(TelegramMediaFile mediaFile)
    {
        var key = _cachedFiles.Add(mediaFile);
        return new(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "Upload to M+",
                    ImageProcessingCallbackData.Serialize(ImageProcessingQueryFlags.UploadImage, key))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "Upscale and upload to M+",
                    ImageProcessingCallbackData.Serialize(ImageProcessingQueryFlags.UpscaleImage | ImageProcessingQueryFlags.UploadImage, key))
            },
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "Vectorize",
                    ImageProcessingCallbackData.Serialize(ImageProcessingQueryFlags.VectorizeImage | ImageProcessingQueryFlags.UploadImage, key))
            }
        });
    }

    InlineKeyboardMarkup CreateReplyMarkupForHighResolutionImage(TelegramMediaFile mediaFile)
    {
        throw new NotImplementedException();
    }
}
