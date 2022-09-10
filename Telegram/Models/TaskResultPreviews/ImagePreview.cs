using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;
using Telegram.Services.Telegram;

namespace Telegram.Models.TaskResultPreviews;

internal class ImagePreview : TaskResultPreview
{
    public int Width;
    public int Height;


    public ImagePreview(JToken mpItem, string executorNodeName) : base(mpItem, executorNodeName)
    {
        var imageDimensions = mpItem["media"]!["jpeg"]!;
        Width = (int)imageDimensions["width"]!;
        Height = (int)imageDimensions["height"]!;
    }


    internal override async Task SendWith(TelegramBot bot, ChatId chatId) =>
        await bot.TrySendImageAsync(chatId, ThumbnailMediumUrl, Caption);
}
