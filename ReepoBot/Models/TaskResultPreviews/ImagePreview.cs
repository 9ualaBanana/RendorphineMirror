using System.Text.Json;

namespace ReepoBot.Models.TaskResultPreviews;

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

    public ImagePreview(JsonElement mpItem)
    {
        var basicMetadata = mpItem.GetProperty("metadata").GetProperty("basic");
        Title = basicMetadata.GetProperty("title").GetString()!;
        Description = basicMetadata.GetProperty("description").GetString()!;
        TaskId = mpItem.GetProperty("id").GetString()!;
        MpIid = mpItem.GetProperty("iid").GetString()!;

        ThumbnailSmallUrl = mpItem.GetProperty("thumbnailurl").GetString()!;
        ThumbnailMediumUrl = mpItem.GetProperty("previewurl").GetString()!;
        ThumbnailBigUrl = mpItem.GetProperty("nowmpreviewurl").GetString()!;

        var imageDimensions = mpItem.GetProperty("media").GetProperty("jpeg");
        Width = imageDimensions.GetProperty("width").GetInt32();
        Height = imageDimensions.GetProperty("height").GetInt32();
    }
}
