using Newtonsoft.Json.Linq;

namespace Telegram.Models.TaskResultPreviews;

public class ImagePreview
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

    public ImagePreview(JToken mpItem)
    {
        var basicMetadata = mpItem["metadata"]!["basic"]!;
        Title = (string)basicMetadata["title"]!;
        Description = (string)basicMetadata["description"]!;
        TaskId = (string)mpItem["id"]!;
        MpIid = (string)mpItem["iid"]!;

        ThumbnailSmallUrl = (string)mpItem["thumbnailurl"]!;
        ThumbnailMediumUrl = (string)mpItem["previewurl"]!;
        ThumbnailBigUrl = (string)mpItem["nowmpreviewurl"]!;

        var imageDimensions = mpItem["media"]!["jpeg"]!;
        Width = (int)imageDimensions["width"]!;
        Height = (int)imageDimensions["height"]!;
    }
}
