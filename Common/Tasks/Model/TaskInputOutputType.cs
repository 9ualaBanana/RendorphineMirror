using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.Tasks.Model;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskInputType
{
    User,
    MPlus,
    DownloadLink,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskOutputType
{
    User,
    MPlus,
}
