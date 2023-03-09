using Newtonsoft.Json.Linq;

namespace Telegram.MPlus;

/// <summary>Represents info for a file stored on M+.</summary>
/// <remarks>Unserialized info can be accessed via indexer.</remarks>
internal class MPlusFileInfo
{
    readonly JToken _json;

    internal readonly string Title;
    internal readonly string Description;
    internal readonly string Iid;
    internal readonly MPlusFileType Type;
    internal readonly Uri SmallThumbnailUrl;
    internal readonly Uri MediumThumbnailUrl;
    internal readonly Uri BigThumbnailUrl;

    internal static MPlusFileInfo From(JToken json) => new(json);

    /// <param name="json">
    /// JSON representing info for a file stored on M+ that can be requested using one of the M+ API endpoints.
    /// </param>
    MPlusFileInfo(JToken json)
    {
        _json = json;

        var basicInfo = _json["metadata"]!["basic"]!;
        Title = (string)basicInfo["title"]!;
        Description = (string)basicInfo["description"]!;

        Iid = (string)_json["iid"]!;
        Type = Enum.Parse<MPlusFileType>((string)_json["type"]!, ignoreCase: true)!;
        SmallThumbnailUrl = (Uri)_json["thumbnailurl"]!;
        MediumThumbnailUrl = (Uri)_json["previewurl"]!;
        BigThumbnailUrl = (Uri)_json["nowmpreviewurl"]!;
    }

    internal JToken? this[string key] => _json[key];

    public static implicit operator JToken(MPlusFileInfo mPlusFileInfo) => mPlusFileInfo._json;
}
