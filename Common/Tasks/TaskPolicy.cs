using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common.Tasks;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskPolicy
{
    AllNodes,
    OwnNodes,
    SameNode,
}
