using System.Text.Json;

namespace ReepoBot.Models.TaskResultPreviews;

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

    public VideoPreview(JsonElement mpItem)
    {
        var basicMetadata = mpItem.GetProperty("metadata").GetProperty("basic");
        Title = basicMetadata.GetProperty("title").GetString()!;
        Description = basicMetadata.GetProperty("description").GetString()!;
        TaskId = mpItem.GetProperty("id").GetString()!;
        MpIid = mpItem.GetProperty("iid").GetString()!;

        ThumbnailSmallUrl = mpItem.GetProperty("thumbnailurl").GetString()!;
        ThumbnailMediumUrl = mpItem.GetProperty("previewurl").GetString()!;
        ThumbnailBigUrl = mpItem.GetProperty("nowmpreviewurl").GetString()!;

        var videoPreview = mpItem.GetProperty("videopreview");
        Width = videoPreview.GetProperty("width").GetInt32();
        Height = videoPreview.GetProperty("height").GetInt32();

        Mp4Size = videoPreview.GetProperty("mp4").GetProperty("size").GetInt64();
        Mp4Url = videoPreview.GetProperty("mp4").GetProperty("url").GetString()!;

        WebmSize = videoPreview.GetProperty("webm").GetProperty("size").GetInt64();
        WebmUrl = videoPreview.GetProperty("webm").GetProperty("url").GetString()!;
    }
}
