using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.Tasks;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskState
{
    Queued,
    Input,
    Active,
    Output,
    Finished,
    Canceled,
    Failed,
}
