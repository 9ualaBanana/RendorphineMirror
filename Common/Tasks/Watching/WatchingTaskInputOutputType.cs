using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.Tasks.Watching;

[JsonConverter(typeof(StringEnumConverter))]
public enum WatchingTaskInputOutputType
{
    Local,
    MPlus,
    MPlusAllFiles,
    OtherNodeTorrent,
    QSPreview,
}