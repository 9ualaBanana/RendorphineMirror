using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;
using Telegram.Services.Telegram;

namespace Telegram.Models.TaskResultPreviews;

public class MpItem
{
    readonly JToken _jsonElement;
    readonly string _executorNodeName;


    internal MpItem(JToken mpItem, string executorNodeName)
    {
        _jsonElement = mpItem;
        _executorNodeName = executorNodeName;
    }


    public string Type => (string)_jsonElement["type"]!;

    public bool IsVideo => Type == "video";
    public bool IsImage => Type == "raster";

    public async Task SendWith(TelegramBot bot, ChatId chatId)
    {
        TaskResultPreview taskResultPreview;
        if (IsVideo) taskResultPreview = AsVideoPreview;
        else if (IsImage) taskResultPreview = AsImagePreview;
        else { await bot.TrySendMessageAsync(chatId, "Unknown type of the resulting M+ item."); return; }

        await taskResultPreview.SendWith(bot, chatId);
    }

    internal VideoPreview AsVideoPreview => new(_jsonElement, _executorNodeName);
    internal ImagePreview AsImagePreview => new(_jsonElement, _executorNodeName);
}
