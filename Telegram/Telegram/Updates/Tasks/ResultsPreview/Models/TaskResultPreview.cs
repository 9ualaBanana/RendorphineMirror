using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

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

    protected string Caption;

    protected InlineKeyboardMarkup DownloadButton;


    internal TaskResultPreview(JToken mpItem, string executorNodeName, string downloadUri)
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

        Caption = $"{Title}\n\nNode: *{ExecutorNodeName}*\nTask ID: *{TaskId}*\nM+ IID: *{MpIid}*";
        DownloadButton = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Download", downloadUri));
    }

    internal abstract Task SendWith(TelegramBot bot, ChatId chatId);
}
