using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NodeCommon.Tasks.Model;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskInputType
{
    MPlus,
    DownloadLink,
    Torrent,
    User,
    DirectUpload,
    TitleKeywords,
}
[JsonConverter(typeof(StringEnumConverter))]
public enum TaskOutputType
{
    MPlus,
    Torrent,
    User,
    QSPreview,
    DirectDownload,
    TitleKeywords,
}