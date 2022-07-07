using System.Text.Json;

namespace Common.Tasks.Models;

public record TaskInfo(
    string UserId,
    long Registered,
    TaskObject Object,
    JsonElement Input,
    JsonElement Output,
    JsonElement Data,
    JsonElement State,
    int Progress,
    string Origin,
    ServerInfo? Server)
{
}
