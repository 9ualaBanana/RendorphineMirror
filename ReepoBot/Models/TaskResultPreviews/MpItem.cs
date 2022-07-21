using System.Text.Json;

namespace ReepoBot.Models.TaskResultPreviews;

public class MpItem
{
    readonly JsonElement _jsonElement;

    public MpItem(JsonElement mpItem)
    {
        _jsonElement = mpItem;
    }

    public string Type => _jsonElement.GetProperty("type").GetString()!;

    public bool IsVideo => Type == "video";

    public VideoPreview AsVideoPreview => new(_jsonElement); 
}
