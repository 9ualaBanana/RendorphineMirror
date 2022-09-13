using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;
using Telegram.Telegram;

namespace Telegram.Telegram.Updates.Tasks.ResultsPreview.Models;

internal abstract class TaskResultPreview
{
    public string Title;
    public string Description;
    public string TaskId;
    public string MpIid;

    public string ThumbnailSmallUrl;
    public string ThumbnailMediumUrl;
    public string ThumbnailBigUrl;

    public string ExecutorNodeName;


    internal TaskResultPreview(JToken mpItem, string executorNodeName)
    {
        var basicMetadata = mpItem["metadata"]!["basic"]!;
        Title = (string)basicMetadata["title"]!;
        Description = (string)basicMetadata["description"]!;
        TaskId = (string)mpItem["id"]!;
        MpIid = (string)mpItem["iid"]!;

        ThumbnailSmallUrl = (string)mpItem["thumbnailurl"]!;
        ThumbnailMediumUrl = (string)mpItem["previewurl"]!;
        ThumbnailBigUrl = (string)mpItem["nowmpreviewurl"]!;

        ExecutorNodeName = executorNodeName;
    }


    protected string Caption => $"{Title}\n\nNode: {ExecutorNodeName}\nTask ID: *{TaskId}*\nM+ IID: *{MpIid}*";
    internal abstract Task SendWith(TelegramBot bot, ChatId chatId);
}
