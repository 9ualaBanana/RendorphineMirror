namespace Node.Tasks.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum TaskPolicy
{
    AllNodes,
    OwnNodes,
    SameNode,
}
