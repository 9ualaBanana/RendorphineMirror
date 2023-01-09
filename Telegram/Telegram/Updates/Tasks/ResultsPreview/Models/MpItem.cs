using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;

public class MpItem
{
    readonly JToken _jsonElement;
    readonly string _executorNodeName;
    readonly string _downloadUri;


    internal MpItem(JToken mpItem, string executorNodeName, string downloadUri)
    {
        _jsonElement = mpItem;
        _executorNodeName = executorNodeName;
        _downloadUri = downloadUri;
    }


    public string Type => (string)_jsonElement["type"]!;

    public bool IsVideo => Type == "video";
    public bool IsImage => Type == "raster" || Type == "vector";

    public async Task SendWith(TelegramBot bot, ChatId chatId)
    {
        TaskResultPreview taskResultPreview;
        if (IsVideo) taskResultPreview = AsVideoPreview;
        else if (IsImage) taskResultPreview = AsImagePreview;
        else { await bot.TrySendMessageAsync(chatId, "Unknown type of the resulting M+ item."); return; }

        await taskResultPreview.SendWith(bot, chatId);
    }

    internal VideoPreview AsVideoPreview => new(_jsonElement, _executorNodeName, _downloadUri);
    internal ImagePreview AsImagePreview => new(_jsonElement, _executorNodeName, _downloadUri);
}
