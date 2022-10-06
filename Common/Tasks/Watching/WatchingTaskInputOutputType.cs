using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.Tasks.Watching;

[JsonConverter(typeof(StringEnumConverter))]
public enum WatchingTaskInputType
{
    Local,
    MPlus,
    MPlusAllFiles,
    OtherNode,
}
[JsonConverter(typeof(StringEnumConverter))]
public enum WatchingTaskOutputType
{
    Local,
    MPlus,
    OtherNodeTorrent,
    QSPreview,
}