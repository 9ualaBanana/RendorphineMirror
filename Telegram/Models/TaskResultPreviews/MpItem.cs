using Newtonsoft.Json.Linq;

namespace Telegram.Models.TaskResultPreviews;

public class MpItem
{
    readonly JToken _jsonElement;

    public MpItem(JToken mpItem)
    {
        _jsonElement = mpItem;
    }

    public string Type => (string)_jsonElement["type"]!;

    public bool IsVideo => Type == "video";
    public bool IsImage => Type == "raster";

    public VideoPreview AsVideoPreview => new(_jsonElement);
    public ImagePreview AsImagePreview => new(_jsonElement);
}
