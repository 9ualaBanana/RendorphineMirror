using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Models;
using Telegram.Telegram.FileRegistry;
using Telegram.Telegram.Updates.Images.Models;

namespace Telegram.Telegram.Updates.Images.Services;

public class TelegramVideoHandler : TelegramUpdateHandler
{
    readonly CachedMediaFiles _fileRegistry;


    public TelegramVideoHandler(ILogger<TelegramImageHandler> logger, TelegramBot bot, CachedMediaFiles fileRegistry)
        : base(logger, bot)
    {
        _fileRegistry = fileRegistry;
    }


    public override async Task HandleAsync(Update update)
    {
        await Bot.SendMessageAsync_(
            update.Message!.Chat.Id,
            "*Choose how to process the video*",
            // Wrap From in try-catch block.
            replyMarkup: CreateReplyMarkupFor(MediaFile.From(update.Message)));
    }

    InlineKeyboardMarkup CreateReplyMarkupFor(MediaFile mediaFile)
    {
        var key = _fileRegistry.Add(mediaFile);
        return new(new InlineKeyboardButton[][]
        {
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "Upload to M+",
                    VideoProcessingCallbackData.Serialize(VideoProcessingQueryFlags.UploadVideo, key))
            }
        });
    }
}
