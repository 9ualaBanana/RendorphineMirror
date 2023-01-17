using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram.FileRegistry;
using Telegram.Telegram.Updates.Images.Models;

namespace Telegram.Telegram.Updates.Images.Services;

public class TelegramVideoHandler : TelegramUpdateHandler
{
    readonly TelegramFileRegistry _fileRegistry;


    public TelegramVideoHandler(ILogger<TelegramImageHandler> logger, TelegramBot bot, TelegramFileRegistry fileRegistry)
        : base(logger, bot)
    {
        _fileRegistry = fileRegistry;
    }


    public override async Task HandleAsync(Update update)
    {
        await Bot.TrySendMessageAsync(
            update.Message!.Chat.Id,
            "*Choose how to process the video*",
            replyMarkup: CreateReplyMarkupFor(TelegramMediaFile.From(update.Message)));
    }

    InlineKeyboardMarkup CreateReplyMarkupFor(TelegramMediaFile mediaFile)
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
