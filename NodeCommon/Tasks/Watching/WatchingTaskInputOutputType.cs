using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NodeCommon.Tasks.Watching;

[JsonConverter(typeof(StringEnumConverter))]
public enum WatchingTaskInputType
{
    Local,
    MPlus,
    MPlusAllFiles,
    OtherNode,
    RectReleases,
    OneClick,
}
[JsonConverter(typeof(StringEnumConverter))]
public enum WatchingTaskOutputType
{
    Torrent,
    MPlus,
    QSPreview,
}
