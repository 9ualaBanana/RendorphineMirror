using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Telegram;

namespace Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;

internal class VideoPreview : TaskResultPreview
{
    public int Width;
    public int Height;

    public long Mp4Size;
    public string Mp4Url;

    public long WebmSize;
    public string WebmUrl;


    public VideoPreview(JToken mpItem, string executorNodeName, string downloadUri)
        : base(mpItem, executorNodeName, downloadUri)
    {
        var videoPreview = mpItem["videopreview"]!;
        Width = (int)videoPreview["width"]!;
        Height = (int)videoPreview["height"]!;

        Mp4Size = (long)videoPreview["mp4"]!["size"]!;
        Mp4Url = (string)videoPreview["mp4"]!["url"]!;

        WebmSize = (long)videoPreview["webm"]!["size"]!;
        WebmUrl = (string)videoPreview["webm"]!["url"]!;
    }


    internal override async Task SendWith(TelegramBot bot, ChatId chatId) =>
        await bot.TrySendVideoAsync(chatId, Mp4Url, ThumbnailMediumUrl, Caption, Width, Height, DownloadButton);
}
