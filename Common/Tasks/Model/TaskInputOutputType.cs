using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.Tasks.Model;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskInputOutputType
{
    MPlus,
    DownloadLink,
    Torrent,
    User,
    QSPreview,
}