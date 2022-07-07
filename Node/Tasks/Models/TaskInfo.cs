using System.Text.Json;

namespace Node.Tasks.Models;

public record TaskInfo(
    string UserId,
    long Registered,
    TaskObject Object,
    JsonElement Input,
    JsonElement Output,
    JsonElement State,
    int Progress,
    string Origin,
    ServerInfo? Server)
{
}
