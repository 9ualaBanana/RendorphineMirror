using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Services.Telegram.FileRegistry;

namespace Telegram.Services.Telegram.Updates.Images;

public class TelegramImageHandler
{
    readonly ILogger _logger;
    readonly TelegramBot _bot;
    readonly TelegramFileRegistry _fileRegistry;

    public TelegramImageHandler(ILogger<TelegramImageHandler> logger, TelegramBot bot, TelegramFileRegistry fileRegistry)
    {
        _logger = logger;
        _bot = bot;
        _fileRegistry = fileRegistry;
    }

    public async Task HandleAsync(Update update)
    {
        var image = update.Message!.Document is not null ?
            TelegramImage.From(update.Message.Document!) : TelegramImage.From(update.Message.Photo!.Last());

        //if (image.Size < 1_000_000)
        //{ await _bot.TrySendMessageAsync(update.Message.Chat.Id, "Resolution of the image must be at least 1 MP."); return; }

        await _bot.TrySendMessageAsync(update.Message.Chat.Id, "*Choose how to process the image*", replyMarkup: CreateReplyMarkupForLowResolutionImage(image));
    }

    InlineKeyboardMarkup ReplyMarkupFor(TelegramImage image) => image.Size switch
    {
        < 20_000_000 => CreateReplyMarkupForLowResolutionImage(image),
        _ => CreateReplyMarkupForHighResolutionImage(image)
    };

    InlineKeyboardMarkup CreateReplyMarkupForLowResolutionImage(TelegramImage image)
    {
        var key = _fileRegistry.Add(image.FileId);
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
