using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Services.Telegram.FileRegistry;

namespace Telegram.Services.Telegram.Updates.Images;

public class TelegramImageHandler : TelegramUpdateHandler
{
    readonly TelegramFileRegistry _fileRegistry;



    public TelegramImageHandler(ILogger<TelegramImageHandler> logger, TelegramBot bot, TelegramFileRegistry fileRegistry)
        : base(logger, bot)
    {
        _fileRegistry = fileRegistry;
    }



    public override async Task HandleAsync(Update update)
    {
        await Bot.TrySendMessageAsync(
            update.Message!.Chat.Id,
            "*Choose how to process the image*",
            replyMarkup: CreateReplyMarkupForLowResolutionImage(TelegramImage.From(update.Message)));
    }

    InlineKeyboardMarkup ReplyMarkupFor(TelegramImage image) => image.Size switch
    {
        < 20_000_000 => CreateReplyMarkupForLowResolutionImage(image),
        _ => CreateReplyMarkupForHighResolutionImage(image)
    };

    InlineKeyboardMarkup CreateReplyMarkupForLowResolutionImage(TelegramImage image)
    {
        var key = _fileRegistry.Add(image.InputOnlineFile);
        return new(new InlineKeyboardButton[][]
        {
            //new InlineKeyboardButton[]
            //{
            //    InlineKeyboardButton.WithCallbackData(
            //        "Upload to M+",
            //        ImageProcessingCallbackData.Serialize(ImageProcessingActions.Upload, key))
            //},
            new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "Upscale and upload to M+",
                    ImageProcessingCallbackData.Serialize(ImageProcessingQueryFlags.Upscale | ImageProcessingQueryFlags.Upload, key))
            }
        });
    }

    InlineKeyboardMarkup CreateReplyMarkupForHighResolutionImage(TelegramImage image)
    {
        throw new NotImplementedException();
    }
}
