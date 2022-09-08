using Newtonsoft.Json.Linq;

namespace Telegram.Models.TaskResultPreviews;

public class VideoPreview
{
    public string Title;
    public string Description;
    public string TaskId;
    public string MpIid;

    public string ThumbnailSmallUrl;
    public string ThumbnailMediumUrl;
    public string ThumbnailBigUrl;

    public int Width;
    public int Height;

    public long Mp4Size;
    public string Mp4Url;

    public long WebmSize;
    public string WebmUrl;

    public VideoPreview(JToken mpItem)
    {
        var basicMetadata = mpItem["metadata"]!["basic"]!;
        Title = (string)basicMetadata["title"]!;
        Description = (string)basicMetadata["description"]!;
        TaskId = (string)mpItem["id"]!;
        MpIid = (string)mpItem["iid"]!;

        ThumbnailSmallUrl = (string)mpItem["thumbnailurl"]!;
        ThumbnailMediumUrl = (string)mpItem["previewurl"]!;
        ThumbnailBigUrl = (string)mpItem["nowmpreviewurl"]!;

        var videoPreview = mpItem["videopreview"];
        Width = (int)videoPreview["width"]!;
        Height = (int)videoPreview["height"]!;

        Mp4Size = (long)videoPreview["mp4"]!["size"]!;
        Mp4Url = (string)videoPreview["mp4"]!["url"]!;

        WebmSize = (long)videoPreview["webm"]!["size"]!;
        WebmUrl = (string)videoPreview["webm"]!["url"]!;
    }
}
