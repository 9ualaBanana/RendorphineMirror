using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Services.Telegram.Authentication;
using Telegram.Services.Telegram.FileRegistry;
using Telegram.Services.Telegram.Updates.Images;

namespace Telegram.Services.Telegram.Updates.Commands;

public class ProcessCommand : AuthenticatedCommand
{
    readonly TelegramFileRegistry _fileRegistry;

    public ProcessCommand(ILogger<ProcessCommand> logger, TelegramBot bot, TelegramChatIdAuthentication authentication, TelegramFileRegistry fileRegistry)
        : base(logger, bot, authentication)
    {
        _fileRegistry = fileRegistry;
    }

    public override string Value => "process";

    protected override async Task HandleAsync(Update update, TelegramAuthenticationToken _)
    {
        if (!ContainsImage(update.Message!))
        { await Bot.TrySendMessageAsync(update.Message!.Chat.Id, "The message doesn't contain an image to process."); return; }

        var image = update.Message!.Document is not null ?
            TelegramImage.From(update.Message.Document!) : TelegramImage.From(update.Message.Photo!.Last());

        //if (image.Size < 1_000_000)
        //{ await _bot.TrySendMessageAsync(update.Message.Chat.Id, "Resolution of the image must be at least 1 MP."); return; }

        await Bot.TrySendMessageAsync(update.Message.Chat.Id, "*Choose how to process the image*", replyMarkup: CreateReplyMarkupForLowResolutionImage(image));
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

    static bool ContainsImage(Message message) => IsImage(message.Document) || message.Photo is not null;

    static bool IsImage(Document? document) =>
        document is not null && document.MimeType is not null && document.MimeType.StartsWith("image");
}
