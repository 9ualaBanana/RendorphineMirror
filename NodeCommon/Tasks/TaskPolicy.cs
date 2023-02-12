using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NodeCommon.Tasks;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskPolicy
{
    AllNodes,
    OwnNodes,
    SameNode,
}
