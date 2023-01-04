using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;

namespace Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;

internal class ImagePreview : TaskResultPreview
{
    public int Width;
    public int Height;


    public ImagePreview(JToken mpItem, string executorNodeName, string downloadUri)
        : base(mpItem, executorNodeName, downloadUri)
    {
        var imageDimensions = mpItem["media"]!["jpeg"]!;
        Width = (int)imageDimensions["width"]!;
        Height = (int)imageDimensions["height"]!;
    }


    internal override async Task SendWith(TelegramBot bot, ChatId chatId) =>
        await bot.TrySendImageAsync(chatId, ThumbnailMediumUrl, Caption);
}
