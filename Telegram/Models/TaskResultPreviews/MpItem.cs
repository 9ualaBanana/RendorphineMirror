using System.Text.Json;

namespace Telegram.Models.TaskResultPreviews;

public class MpItem
{
    readonly JsonElement _jsonElement;

    public MpItem(JsonElement mpItem)
    {
        _jsonElement = mpItem;
    }

    public string Type => _jsonElement.GetProperty("type").GetString()!;

    public bool IsVideo => Type == "video";
    public bool IsImage => Type == "raster";

    public VideoPreview AsVideoPreview => new(_jsonElement);
    public ImagePreview AsImagePreview => new(_jsonElement);
}
