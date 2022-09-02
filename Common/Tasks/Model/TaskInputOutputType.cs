using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.Tasks.Model;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskInputType
{
    MPlus,
    DownloadLink,
    Torrent,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskOutputType
{
    MPlus,
    Torrent,
}
